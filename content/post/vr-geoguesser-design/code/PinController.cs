using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using static VRC.Core.ApiAvatar;
using VRC.SDK3.Components;


[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PinController : UdonSharpBehaviour
{
    [Header("References")]
    public LatLongMapper latLongMapper;
    public RectTransform mapRectTransform;
    public GameManager gameManager;
    public TextMeshProUGUI ownername; // 显示所有者名字
    public TextMeshProUGUI coordinateText; // 显示经纬度
    public TextMeshProUGUI debugText; // 显示调试信息

    private VRCPlayerApi _owner; // 所有者
    private VRCPickup pickup; // 拾取组件
    [UdonSynced]
    private bool canPickup = false; // 只有所有者才能拾取

    [Header("Visual Settings")]
    public float hoverHeight = 0.1f;

   
    private Vector3 _originalPosition;
    private bool _isPickedUp;
    private Rigidbody _rigidbody;

    public PinDataManager pinDataManager;

    // 在 PinController.cs 类中添加以下变量
    [Header("Line Renderer Settings")]
    public bool showLineToAnswer = false; // 是否显示到答案的连线
    public Material lineMaterial; // 线条材质
    public Color lineColor = Color.red; // 线条颜色
    public float lineWidth = 0.5f; // 线条宽度

    private LineRenderer lineRenderer; // 连线渲染器
    private GameObject answerPinObject; // 答案Pin的引用

    private bool isPlacedOnMap = false;

    [Header("音频设置")]
    public AudioSource audioSource;  // 音频源组件
    public AudioClip pickupSound;    // 拾取音效
    public AudioClip dropSound;      // 放下音效


    private void Start()
    {

        // 自动从父级获取 GameManager
        if (gameManager == null)
        {
            gameManager = GetComponentInParent<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[PinController] 无法找到 GameManager，确保 Prefab 在 GameManager 之下！");
            }
            else
            {
                Debug.Log($"[PinController] 成功找到 GameManager: {gameManager.name}");
            }
        }

        // **修正 LatLongMapper 查找方式**
        if (latLongMapper == null)
        {
            latLongMapper = transform.root.GetComponentInChildren<LatLongMapper>(true);
            if (latLongMapper == null)
            {
                Debug.LogError("[PinController] 无法找到 LatLongMapper，请检查场景层级结构！");
            }
            else
            {
                Debug.Log($"[PinController] 成功找到 LatLongMapper: {latLongMapper.name}");
            }
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogWarning("[PinController] 未找到 LineRenderer 组件，正在尝试添加...");
                // 在 UdonSharp 中不能直接添加组件，所以这里只能输出警告
            }
            else
            {
                Debug.Log("[PinController] 成功找到 LineRenderer 组件！");
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("[PinController] 未找到 AudioSource组件，正在尝试添加...");
                // 在 UdonSharp 中不能直接添加组件，所以这里只能输出警告
            }
            else
            {
                Debug.Log("[PinController] 成功找到 AudioSource组件！");
            }
        }

        // 自动获取 PinDataManager
        if (pinDataManager == null)
        {
            pinDataManager = GetComponentInParent<PinDataManager>();
            if (pinDataManager == null)
            {
                Debug.LogError("[PinController] 无法找到 PinDataManager，确保它存在于 GameManager 或同级对象中！");
            }
            else
            {
                Debug.Log($"[PinController] 成功找到 PinDataManager: {pinDataManager.name}");
            }
        }

        // **修正 mapRectTransform 查找方式**
        if (mapRectTransform == null)
        {
            //mapRectTransform = transform.root.GetComponentInChildren<RectTransform>(true);
            mapRectTransform = GameObject.Find("WorldSatelliteMap").GetComponent<RectTransform>();

            if (mapRectTransform == null)
            {
                Debug.LogError("[PinController] 无法找到 mapRectTransform，请检查场景层级结构！");
            }
            else
            {
                Debug.Log($"[PinController] 成功找到 mapRectTransform: {mapRectTransform.name}");
            }
        }

        if (debugText == null)
        {
            // 在整个场景中查找所有 TextMeshProUGUI 组件
            TextMeshProUGUI[] allTextComponents = transform.root.GetComponentsInChildren<TextMeshProUGUI>(true);

            // 遍历找到名字为 "DebugText" 的组件
            foreach (var textComponent in allTextComponents)
            {
                if (textComponent.name == "DebugText") // 确保名字匹配
                {
                    debugText = textComponent;
                    Debug.Log($"[PinController] 成功找到 debugText: {debugText.name}");
                    break;
                }
            }

            // 如果仍未找到，打印错误信息
            if (debugText == null)
            {
                Debug.LogError("[PinController] 无法找到 debugText，请检查层级结构是否正确！");
            }
        }


        // 获取组件引用
        _owner = Networking.GetOwner(gameObject);
        SetPinVisibility(Networking.LocalPlayer == _owner);

        _rigidbody = GetComponent<Rigidbody>(); // 获取刚体组件
        pickup = GetComponent<VRCPickup>(); // 获取拾取组件

        // 添加启动时的调试信息
        string startDebugInfo = "Pin初始化:\n";
        startDebugInfo += $"初始所有者: {_owner.displayName}\n";
        startDebugInfo += $"本地玩家: {Networking.LocalPlayer.displayName}\n"; 

        startDebugInfo += $"初始所有者id: {_owner.playerId }\n";
        startDebugInfo += $"本地玩家id: {Networking.LocalPlayer.playerId}\n";

        if (pickup != null)
        {
            pickup.pickupable = (Networking.LocalPlayer == _owner);
            startDebugInfo += $"初始Pickup状态: {pickup.pickupable}";
        }
        else
        {
            startDebugInfo += "错误: pickup组件为null";
        }

        //Debug.Log(startDebugInfo);
        debugText.text = startDebugInfo;

        UpdateOwnerName();
        _originalPosition = transform.position;

        // 获取或初始化LineRenderer
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            Debug.LogWarning("[PinController] 未设置LineRenderer，请在Inspector中手动设置。");
        }

        if (lineRenderer != null)
        {
            // 初始化LineRenderer设置
            lineRenderer.positionCount = 2; // 起点和终点
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            // 设置材质和颜色
            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }

            // 设置颜色
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            // 默认不显示
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        // 保持Pin垂直
        if (_isPickedUp)
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }

    public override void OnPickup()
    {

        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        _isPickedUp = true;
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }
    }

    public override void OnDrop()
    {
        if (audioSource != null && dropSound != null)
        {
            audioSource.PlayOneShot(dropSound);
        }

        ////Debug.Log("OnDrop");
        _isPickedUp = false;
        
        // 重置物理属性
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
        }

        // 保持垂直
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

        // 更新连线位置, 目前我认为没有必要
        //UpdateLinePosition();

    }

    private void SetPinVisibility(bool isOwner) // 设置 `Pin` 的可见性
    {
        ////int visibleLayer = LayerMask.NameToLayer("Pinlayer"); // 可见
        ////int invisibleLayer = LayerMask.NameToLayer("InvisiblePinLayer"); // 不可见
        //int visibleLayer = 23; // 可见
        //int invisibleLayer = 24; // 不可见

        ////Debug.Log($"[Pin] 设置 `Pin` Layer 为: {(isOwner ? "PinLayer" : "InvisiblePinLayer")}");

        //gameObject.layer = isOwner ? visibleLayer : invisibleLayer;


        //// 让所有子对象的 Layer 也同步
        //foreach (Transform child in gameObject.transform)
        //{
        //    child.gameObject.layer = isOwner ? visibleLayer : invisibleLayer;
        //}

        // 2️⃣ 设置 `Renderer` 透明度（适用于 3D 物体）
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = isOwner ? 1.0f : 0.0f;  // 非 Owner 透明
                mat.color = color;

                // 确保透明模式
                mat.SetFloat("_Mode", isOwner ? 0 : 3);  // 0: Opaque, 3: Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", isOwner ? 1 : 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = isOwner ? -1 : 3000;
            }
        }

        // 3️⃣ 设置 `CanvasGroup` 透明度（适用于 2D UI）
        CanvasGroup[] canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = isOwner ? 1.0f : 0.0f;
            canvasGroup.interactable = isOwner;
            canvasGroup.blocksRaycasts = isOwner;
        }

    }

    // 更新可见性的公共方法
    public void UpdateVisibility(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = isVisible ? 1.0f : 0.0f;
                mat.color = color;

                mat.SetFloat("_Mode", isVisible ? 0 : 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", isVisible ? 1 : 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = isVisible ? -1 : 3000;
            }
        }

        CanvasGroup[] canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = isVisible ? 1.0f : 0.0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
    }

    // 在 PinController.cs 中修改 SetAnswerPin 方法
    public void SetAnswerPin(GameObject answerPin)
    {
        // 添加详细的日志
        //Debug.Log($"[Pin] SetAnswerPin 被调用 - answerPin: {(answerPin != null ? answerPin.name : "null")}");

        answerPinObject = answerPin;

        // 验证设置是否成功
        //Debug.Log($"[Pin] 设置后的 answerPinObject: {(answerPinObject != null ? answerPinObject.name : "null")}");
    }

    // 修改 ShowLineToAnswer 方法，让它直接更新连线位置
    public void ShowLineToAnswer(bool show)
    {
        // 这个确实激活的
        //Debug.Log($"[Pin] ShowLineToAnswer: {show}");
        showLineToAnswer = show;

        // 先检查LineRenderer是否存在
        if (lineRenderer == null)
        {
            Debug.LogWarning("[Pin] lineRenderer 为 null，无法显示连线");
            return;
        }

        // 检查answerPinObject是否为null
        if (answerPinObject == null)
        {
            Debug.LogWarning("[Pin] answerPinObject 为 null，无法显示连线");
            // 确保LineRenderer被禁用
            lineRenderer.enabled = false;
            return;
        }

        // 只有在pin实际与地图有碰撞时才显示连线
        if (show && isPlacedOnMap)
        {
            // 只在状态变化时更新一次位置
            lineRenderer.enabled = true;
            //Debug.Log($"[Pin] 更新连线位置");
            //Debug.Log($"[Pin] Pin位置: {transform.position}");
            //Debug.Log($"[Pin] 答案Pin位置: {answerPinObject.transform.position}");
            UpdateLinePosition();
            //lineRenderer.SetPosition(0, transform.position);
            //lineRenderer.SetPosition(1, answerPinObject.transform.position);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    // 添加一个方法，可以在需要时手动更新连线位置（比如当Pin移动时）
    public void UpdateLinePosition()
    {

        if (!showLineToAnswer || lineRenderer == null || answerPinObject == null) return;

        // 计算Pin底部位置（假设Y轴向上）
        // 获取Pin的高度（可以是Mesh的高度或者预设的值）
        float pinHeight = 0.6f; // 根据你的Pin实际高度调整
        Vector3 pinBottomPosition = transform.position - new Vector3(0, pinHeight / 2, 0);

        // 同理，计算答案Pin的底部位置
        Vector3 answerPinBottomPosition = answerPinObject.transform.position - new Vector3(0, pinHeight / 2, 0);

        // 设置线条位置
        lineRenderer.SetPosition(0, pinBottomPosition);
        lineRenderer.SetPosition(1, answerPinBottomPosition);
    }

    private void HandlePinPlacement()
    {
        // 计算放置位置
        Vector2 localPoint = WorldToLocalPoint(transform.position);

        // 检查是否在地图范围内
        if (IsPointInMapBounds(localPoint))
        {
            // 转换为经纬度并记录位置
            if (latLongMapper != null && _owner != null && gameManager != null)
            {
                Vector2 latLong = latLongMapper.UICoordsToLatLong(localPoint);
                isPlacedOnMap = true; // 标记为已放置
                UpdateCoordinateText(latLong);
                //gameManager.RecordPinPosition(_owner.playerId, latLong);
               
            }
        }
        else
        {
            // 如果不在地图范围内，重置到原始位置
            //Debug.Log("不在地图范围内，重置到原始位置"); 
            isPlacedOnMap = false; // 标记为未放置
            ResetPosition();
           
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"碰撞发生于：{collision.gameObject.name}");
        if (collision.gameObject.name == "MapTable") // 替代 CompareTag
        {
            //Debug.Log($"与MapTable碰撞啦！！！");
            HandlePinPlacement();　// 处理放置
        }
        else {
            // 如果碰到非地图物体，将标记设为未放置状态
            //Debug.Log($"与非地图物体 {collision.gameObject.name} 碰撞，重置放置状态！");
            isPlacedOnMap = false;

            // 如果需要，可以更新UI和数据
            if (coordinateText != null)
            {
                coordinateText.text = "Not Placed";
            }

            // 通知PinDataManager状态更新
            if (pinDataManager != null && _owner != null)
            {
                // 使用当前位置的经纬度，但标记为未放置
                Vector2 currentPosition = new Vector2(0, 0); // 或者使用上次有效的位置
                pinDataManager.UpdatePlayerPinData(_owner.playerId, currentPosition, false);
            }
        }

    }


    private Vector2 WorldToLocalPoint(Vector3 worldPosition)
    {
        if (mapRectTransform == null) return Vector2.zero;
        Vector3 localPosition = mapRectTransform.InverseTransformPoint(worldPosition);
        return new Vector2(localPosition.x, localPosition.y);
    }

    private bool IsPointInMapBounds(Vector2 localPoint)
    {
        if (mapRectTransform == null) return false;
        
        Vector2 size = mapRectTransform.rect.size / 2f;
        return Mathf.Abs(localPoint.x) <= size.x && Mathf.Abs(localPoint.y) <= size.y;
    }

    private void UpdateCoordinateText(Vector2 latLong)
    {
        if (coordinateText != null)
        {
            coordinateText.text = $"Lat: {latLong.x:F2}\nLong: {latLong.y:F2}";
            // 然后让传下当前的经纬度
            pinDataManager.UpdatePlayerPinData(_owner.playerId, latLong, isPlacedOnMap);

        }
    }

    private void ResetPosition()
    {
        transform.position = _originalPosition;
        transform.rotation = Quaternion.identity;

        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        // 重置放置状态
        isPlacedOnMap = false;

        // 更新UI和数据
        if (coordinateText != null)
        {
            coordinateText.text = "Not Placed";
        }

        // 通知PinDataManager状态更新
        if (pinDataManager != null && _owner != null)
        {
            Vector2 defaultPosition = new Vector2(0, 0);
            pinDataManager.UpdatePlayerPinData(_owner.playerId, defaultPosition, false);
        }
    }



    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        // 详细的调试信息
        string debugInfo = $"[Pin] 所有权转移\n";
        debugInfo += $"新所有者: {player.displayName} (ID: {player.playerId})\n";
        debugInfo += $"本地玩家: {Networking.LocalPlayer.displayName} (ID: {Networking.LocalPlayer.playerId})\n";
        debugInfo += $"是否为本地玩家: {Networking.LocalPlayer == player}\n";

        _owner = player;
        SetPinVisibility(Networking.LocalPlayer == _owner);

        if (pickup != null)
        {
            pickup.pickupable = (Networking.LocalPlayer == player);
            //canPickup = (Networking.LocalPlayer == player);
            debugInfo += $"Pickup状态: {pickup.pickupable}\n";
            //debugInfo += $"CanPickup状态: {canPickup}";
        }
        else
        {
            debugInfo += "错误: pickup组件为null";
        }

        //Debug.Log(debugInfo);

        //debugText.text = debugInfo;

        //if (pickup != null)
        //{
        //    pickup.pickupable = canPickup;
        //}

        UpdateOwnerName();
        //RequestSerialization();
        //SendCustomEventDelayedSeconds(nameof(EnablePickup), 0.1f);
    }

    //public void EnablePickup()
    //{
    //    if (pickup != null)
    //    {
    //        pickup.pickupable = canPickup; // 🔥 延迟后再修改 pickupable
    //    }
    //}

    public void ResetPin()
    {
        ResetPosition();
        _isPickedUp = false;
        
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
        }
    }

    private void UpdateOwnerName()
    {
        if (_owner != null && _owner.IsValid())
        {
            // 更新显示文本为所有者的显示名称
            //ownername.text = $"Player {_owner.playerId}: {_owner.displayName}";
            ownername.text = $"{_owner.displayName}";
            //ownername.text = _owner.displayName;
            //Debug.Log($"对象 '{gameObject.name}' 的所有者是: {_owner.displayName}");
        }
        else
        {
            ownername.text = "Invalid owner";
            //Debug.Log($"对象 '{gameObject.name}' 当前没有有效的所有者。");
        }
    }

    public override void OnDeserialization()
    {
        //if (pickup != null)
        //{
        //    pickup.pickupable = canPickup;
        //    Debug.Log($"OnDeserialization触发啦: {pickup.pickupable}");
        //}
    }
}