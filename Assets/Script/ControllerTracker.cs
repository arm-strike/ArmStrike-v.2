using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class ControllerTracker : MonoBehaviour
{
    [Header("--- プレイヤー・コントローラー割り当て設定 ---")]
    [Tooltip("左手なら LeftHand、右手なら RightHand を設定")]
    public XRNode nodeRole;

    [Tooltip("何番目のコントローラーデバイスを使うか設定してください")]
    public int targetIndex = 0;

    [Header("--- 追従・固定の設定 ---")]
    public bool enablePositionTracking = true;
    public bool enableRotationTracking = true;

    [Header("--- 衝突判定（Collider）の設定 ---")]
    public List<Collider> targetColliders = new List<Collider>();

    [Header("--- 突き出し（パンチ）判定設定 ---")]
    public float punchThreshold = 0.1f;
    private float defaultZ = 0f;
    private bool isInitialized = false;
    private bool isPunching = false;

    [HideInInspector] public bool attackDown { get; private set; }

    public bool IsAttacking => isPunching;

    public Vector3 CurrentPosition { get; private set; } = Vector3.zero;
    public float CurrentY => CurrentPosition.y;

    private float initialY = 0f;
    private bool isInitialYSet = false;
    public float DropAmount
    {
        get
        {
            if (!isInitialYSet && CurrentPosition != Vector3.zero)
            {
                initialY = CurrentPosition.y;
                isInitialYSet = true;
            }
            return isInitialYSet ? (initialY - CurrentPosition.y) : 0f;
        }
    }

    private InputDevice targetDevice;
    private bool deviceFound = false;

    // --- エディタ・キーボード操作用のデバッグ用変数 ---
    private Vector3 debugPosOffset = Vector3.zero;

    void Update()
    {
        attackDown = false;

        if (!deviceFound || !targetDevice.isValid)
        {
            FindTargetDevice();
        }

        // --- デバイスが見つからない（またはエディタでテスト中）の場合のキーボード操作 ---
        if (!deviceFound)
        {
            HandleKeyboardDebugInput();
            return;
        }

        // 位置の取得
        if (targetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            CurrentPosition = position;

            if (!isInitialized)
            {
                defaultZ = position.z;
                isInitialized = true;
            }

            if (enablePositionTracking)
            {
                transform.localPosition = position;
            }

            float deltaZ = position.z - defaultZ;

            if (deltaZ > punchThreshold && !isPunching)
            {
                isPunching = true;
                attackDown = true;
            }
            else if (deltaZ < punchThreshold * 0.3f)
            {
                isPunching = false;
            }
        }

        // 回転の取得
        if (targetDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            if (enableRotationTracking)
            {
                transform.localRotation = rotation;
            }
        }
    }

    // --- キーボードによるエディタテスト用ロジック ---
    void HandleKeyboardDebugInput()
    {
        float moveSpeed = 3f * Time.deltaTime;

        if (nodeRole == XRNode.LeftHand)
        {
            if (Input.GetKey(KeyCode.W)) debugPosOffset.z += moveSpeed;
            if (Input.GetKey(KeyCode.S)) debugPosOffset.z -= moveSpeed;

            // ★ Aキーを押している間だけ左に下がる（左回転のトリガー）
            if (Input.GetKey(KeyCode.A)) debugPosOffset.y -= moveSpeed;
            else debugPosOffset.y = Mathf.Lerp(debugPosOffset.y, 0f, Time.deltaTime * 10f);
        }
        else if (nodeRole == XRNode.RightHand)
        {
            if (Input.GetKey(KeyCode.I)) debugPosOffset.z += moveSpeed;
            if (Input.GetKey(KeyCode.K)) debugPosOffset.z -= moveSpeed;

            // ★ Dキーを押している間だけ右に下がる（右回転のトリガー）
            if (Input.GetKey(KeyCode.D)) debugPosOffset.y -= moveSpeed;
            else debugPosOffset.y = Mathf.Lerp(debugPosOffset.y, 0f, Time.deltaTime * 10f);
        }

        // 自動で元の位置に戻る（キーを離したらZを0へ）
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.I) && !Input.GetKey(KeyCode.K))
        {
            debugPosOffset.z = Mathf.Lerp(debugPosOffset.z, 0f, Time.deltaTime * 10f);
        }

        CurrentPosition = new Vector3(transform.localPosition.x, 1.0f + debugPosOffset.y, 0.5f + debugPosOffset.z);
        transform.localPosition = CurrentPosition;

        if (!isInitialized)
        {
            defaultZ = CurrentPosition.z;
            isInitialized = true;
        }

        float deltaZ = CurrentPosition.z - defaultZ;

        // キーボード操作でのパンチ判定
        if (deltaZ > punchThreshold && !isPunching)
        {
            isPunching = true;
            attackDown = true;
            Debug.Log($"<color=cyan>[デバッグ] {nodeRole} のキーボードパンチ発動！ deltaZ: {deltaZ:F2}</color>");
        }
        else if (deltaZ < punchThreshold * 0.3f)
        {
            isPunching = false;
        }
    }

    void FindTargetDevice()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);

        List<InputDevice> validControllers = new List<InputDevice>();

        foreach (var device in allDevices)
        {
            if (device.isValid && ((device.characteristics & InputDeviceCharacteristics.Controller) != 0 ||
                                   (device.characteristics & InputDeviceCharacteristics.TrackedDevice) != 0))
            {
                bool alreadyAdded = false;
                foreach (var existing in validControllers)
                {
                    if (existing.name == device.name && existing.serialNumber == device.serialNumber)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (!alreadyAdded)
                {
                    validControllers.Add(device);
                }
            }
        }

        if (validControllers.Count > targetIndex)
        {
            targetDevice = validControllers[targetIndex];
            deviceFound = true;
        }
    }
}