using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using TMPro.Examples;
using System.Text;
using TMPro;
using Cyan.PlayerObjectPool;
using VRC.Udon.Common.Interfaces;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PinDataManager : UdonSharpBehaviour
{
    public Transform resetTargetTransform; // 拖一个地图中心或初始位置空物体到这里

    // 存储所有玩家的 Pin 经纬度数据
    [UdonSynced] private string playerInfo;
    public TextMeshProUGUI dataListPlayerText;

    // 存储每轮的答案
    private DataList[] roundAnswers;
    [UdonSynced] private string[] serializedRoundAnswers = new string[0]; 
    public TextMeshProUGUI gameDataStoreageText;

    [UdonSynced]
    private int totalRounds = 5; // 与 GameManager 保持一致

    // 显示最终得分
    public TextMeshProUGUI roundScoresText;

    // 添加一个用于存储当前轮次得分的字符串=
    [UdonSynced] 
    private string currentRoundScoresText = "";

    //[UdonSynced]
    private bool showAllPins = false;  // 控制是否显示所有Pin的状态

    // 改为使用 ObjectAssigner 而不是 ObjectPool
    public CyanPlayerObjectAssigner objectAssigner;

    private DataList dataList = new DataList()
    {
        //new DataDictionary()
        //{
        //    { "id", 1 },
        //    { "longitude", 120.123456 },
        //    { "latitude", 30.123456 }
        //},
        // 可以根据需要继续添加更多的数据点
    };
    [UdonSynced] private string serializedData; // 用于同步的 JSON 字符串
    //private DataDictionary playerData; // 存储玩家的经纬度信息
     // 用 DataDictionary 存储玩家分数
    
    DataDictionary playerTotalScores = new DataDictionary();

    // Add this to your existing variable declarations in PinDataManager.cs
    [UdonSynced] private string serializedPlayerScores = ""; // Serialized scores data

    private void Start()
    {

        //if (serializedRoundAnswers == null)
        //{
        //    serializedRoundAnswers = new string[0]; // ✅ 初始化
        //}
        Debug.Log("[PinDataManager] 初始化完成");


        //if (objectAssigner == null)
        //{
        //    Debug.Log($"[PinDataManager] 开始查找 CyanPlayerObjectAssigner");

        //    // 在子级层次结构中查找 CyanPlayerObjectAssigner
        //    objectAssigner = GetComponentInChildren<CyanPlayerObjectAssigner>();

        //    if (objectAssigner != null)
        //    {
        //        Debug.Log($"[PinDataManager] 成功找到 objectAssigner: {objectAssigner.gameObject.name}");
        //    }
        //    else
        //    {
        //        Debug.LogError($"[PinDataManager] 无法找到 CyanPlayerObjectAssigner，请检查子级层次结构！");
        //        return;
        //    }
        //}
    }


    // 新增：设置所有Pin可见性的方法
    public void SetShowAllPins()
    {
        //if (!Networking.IsOwner(gameObject)) return;

        //Debug.Log($"[PinDataManager] 设置所有Pin可见");
        showAllPins = true;
        // 立即在本地执行可见性更新
        //RequestSerialization();  // 会触发其他客户端的 OnDeserialization
        // 应该每一个客户端都调用一次
        UpdatePinVisibility();
        //SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePinVisibility)); // 有可能要在所有客户端执行，onDeserialization不一定会触发
        
    }
    public void SetHideOtherPins()
    {
        //if (!Networking.IsOwner(gameObject)) return;
        //Debug.Log($"[PinDataManager] 设置所有Pin不可见");
        showAllPins = false;
        // 立即在本地执行可见性更新
        //RequestSerialization();  // 会触发其他客户端的 OnDeserialization
        // 应该每一个客户端都调用一次
        UpdatePinVisibility();
        //SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePinVisibility)); // 有可能要在所有客户端执行，onDeserialization不一定会触发
    }

    public void InitializeRounds(int rounds)
    {
        //Debug.Log($"[PinDataManager] 开始初始化回合数: {rounds}");

        totalRounds = rounds;
        roundAnswers = new DataList[totalRounds];
        serializedRoundAnswers = new string[totalRounds];

        // 确保每个元素都被初始化
        for (int i = 0; i < totalRounds; i++)
        {
            roundAnswers[i] = new DataList();
            serializedRoundAnswers[i] = "";
            //Debug.Log($"[PinDataManager] 初始化回合 {i}");
        }
        
        // 同步修改，确保其他客户端也收到更新
        RequestSerialization();

    }

    public void ClearDataList()
    {
        if (!Networking.IsOwner(gameObject)) return;

        // 清空数据列表
        dataList = new DataList();

        // 序列化空的dataList
        if (VRCJson.TrySerializeToJson(dataList, JsonExportType.Minify, out DataToken jsonToken))
        {
            serializedData = jsonToken.String;
        }
        // 更新UI显示
        playerInfo = "No data yet.";
        if (dataListPlayerText != null)
        {
            dataListPlayerText.text = playerInfo;
        }

        // 同步到所有客户端
        RequestSerialization();
    }


    //public void UpdateDataListPlayerText()
    //{
    //    dataListPlayerText.text = playerInfo;
    //    RequestSerialization(); // 请求同步数据
    //}



    // 更新玩家的 Pin 经纬度数据 
    public void UpdatePlayerPinData(int playerId, Vector2 pinCoordinates, bool isPlacedOnMap)
    {
        //Debug.Log($"UpdatePlayerPinData: {playerId}, {pinCoordinates}, placed: {isPlacedOnMap}");

        bool playerFound = false;

        // 遍历 dataList 查找是否存在相同的 playerId
        for (int i = 0; i < dataList.Count; i++)
        {
            if (dataList.TryGetValue(i, TokenType.DataDictionary, out DataToken dataToken))
            {
                DataDictionary dataPoint = dataToken.DataDictionary;
                // 检查当前 DataDictionary 是否包含相同的 playerId
                if (dataPoint.TryGetValue("id", out DataToken idToken)) // 不指定类型
                {
                    int storedId;
                    // 根据实际类型获取值
                    if (idToken.TokenType == TokenType.Int)
                        storedId = idToken.Int;
                    else if (idToken.TokenType == TokenType.Double)
                        storedId = (int)idToken.Double;
                    else
                        storedId = int.Parse(idToken.ToString());

                    if (storedId == playerId)
                    {
                        // 更新经纬度
                        dataPoint.SetValue("latitude", pinCoordinates.x);
                        dataPoint.SetValue("longitude", pinCoordinates.y);
                        dataPoint.SetValue("isPlaced", isPlacedOnMap);
                        playerFound = true;
                        break;
                    }
                }
            }
        }

        // 如果未找到相同的 playerId，则添加新的 DataDictionary
        if (!playerFound)
        {
            DataDictionary newDataPoint = new DataDictionary();
            newDataPoint.SetValue("id", playerId);
            newDataPoint.SetValue("latitude", pinCoordinates.x);
            newDataPoint.SetValue("longitude", pinCoordinates.y);
            newDataPoint.SetValue("isPlaced", isPlacedOnMap);
            dataList.Add(newDataPoint);
        }

        // 序列化 DataList 为 JSON
        if (VRCJson.TrySerializeToJson(dataList, JsonExportType.Minify, out DataToken jsonToken))
        {
            serializedData = jsonToken.String;
            //RequestSerialization(); // 请求同步
        }
        else
        {
            //Debug.LogError("Failed to serialize dataList to JSON.");
        }

        // 更新同步变量 playerInfo
        playerInfo = LogDataListContents();
        dataListPlayerText.text = playerInfo;
        RequestSerialization(); // 请求同步

    }


    //// 获取玩家的 Pin 经纬度数据
    //public Vector2 GetPlayerPinData(string playerId)
    //{
    //    if (playerData != null && playerData.ContainsKey(playerId))
    //    {
    //        return (Vector2)playerData[playerId];
    //    }
    //    return Vector2.zero; // 如果找不到数据，返回默认值
    //}

    public string LogDataListContents()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < dataList.Count; i++)
        {
            if (dataList.TryGetValue(i, TokenType.DataDictionary, out DataToken dataToken))
            {
                DataDictionary dataDict = dataToken.DataDictionary;
                sb.AppendLine($"Index {i}: DataDictionary with {dataDict.Count} entries");

                // 获取所有键
                DataList keys = dataDict.GetKeys();
                for (int j = 0; j < keys.Count; j++)
                {
                    DataToken keyToken = keys[j];
                    string key = keyToken.String;

                    // 获取对应的值
                    if (dataDict.TryGetValue(keyToken, out DataToken valueToken))
                    {
                        string value = valueToken.ToString();
                        sb.AppendLine($"    Key: {key}, Value: {value}");
                    }
                    else
                    {
                        sb.AppendLine($"    Key: {key}, Value: <Unable to retrieve>");
                    }
                }
            }
            else
            {
                sb.AppendLine($"Index {i}: Not a DataDictionary or retrieval failed.");
            }
        }
        // 将收集的字符串分配给 UI 文本
        return sb.ToString();
    }

    //// 保存当前回合的所有玩家答案
    //public void SaveRoundAnswers(int roundIndex)
    //{
    //    // 只有所有者才能保存数据
    //    if (!Networking.IsOwner(gameObject)) return;

    //    // 保存当前的 dataList 到对应回合
    //    roundAnswers[roundIndex] = dataList;

    //    // 序列化当前回合数据
    //    if (VRCJson.TrySerializeToJson(dataList, JsonExportType.Minify, out DataToken jsonToken))
    //    {
    //        serializedRoundAnswers[roundIndex] = jsonToken.String;
    //    }

    //    // 更新 UI 显示， 这个没有必要在所有客户端都更新，因为只有所有者才能保存数据
    //    StringBuilder storageText = new StringBuilder("Game Round Data:\n");
    //    for (int i = 0; i < totalRounds; i++)   
    //    {
    //        storageText.AppendLine($"Round {i + 1}:");
    //        if (roundAnswers[i] != null && roundAnswers[i].Count > 0)  // 检查是否有数据
    //        {
    //            // 遍历该轮次的所有玩家答案
    //            for (int j = 0; j < roundAnswers[i].Count; j++)
    //            {
    //                if (roundAnswers[i].TryGetValue(j, TokenType.DataDictionary, out DataToken dataToken))
    //                {
    //                    DataDictionary playerAnswer = dataToken.DataDictionary;
    //                    if (playerAnswer.TryGetValue("id", TokenType.Int, out DataToken idToken) &&
    //                        playerAnswer.TryGetValue("longitude", TokenType.Float, out DataToken longToken) &&
    //                        playerAnswer.TryGetValue("latitude", TokenType.Float, out DataToken latToken))
    //                    {
    //                        storageText.AppendLine($"Player {idToken.Int}: Lat {latToken.Float:F2}, Long {longToken.Float:F2}");
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            storageText.AppendLine("No data");
    //        }
    //        storageText.AppendLine();
    //    }
    //    gameDataStoreageText.text = storageText.ToString();

    //    // 清空当前回合的数据，准备下一轮
    //    // 其实不需要清空，因为每轮的数据都是独立的
    //    //dataList = new DataList();

    //    // 同步
    //    RequestSerialization();
    //}

    // 保存当前回合的所有玩家答案 - 使用深拷贝避免引用问题
    public void SaveRoundAnswers(int roundIndex)
    {
        // 只有所有者才能保存数据
        if (!Networking.IsOwner(gameObject)) return;

        // 创建新的DataList进行深拷贝
        DataList roundCopy = new DataList();

        // 复制每个数据项
        for (int i = 0; i < dataList.Count; i++)
        {
            if (dataList.TryGetValue(i, out DataToken token))
            {
                // 需要对DataDictionary特殊处理
                if (token.TokenType == TokenType.DataDictionary)
                {
                    DataDictionary originalDict = token.DataDictionary;
                    DataDictionary newDict = new DataDictionary();

                    // 复制字典中的每个键值对
                    DataList keys = originalDict.GetKeys();
                    for (int j = 0; j < keys.Count; j++)
                    {
                        if (originalDict.TryGetValue(keys[j], out DataToken valueToken))
                        {
                            // 根据值的类型进行复制
                            switch (valueToken.TokenType)
                            {
                                case TokenType.Float:
                                    newDict.SetValue(keys[j].String, valueToken.Float);
                                    break;
                                case TokenType.Double:
                                    newDict.SetValue(keys[j].String, valueToken.Double);
                                    break;
                                case TokenType.Int:
                                    newDict.SetValue(keys[j].String, valueToken.Int);
                                    break;
                                case TokenType.String:
                                    newDict.SetValue(keys[j].String, valueToken.String);
                                    break;
                                case TokenType.Boolean:
                                    newDict.SetValue(keys[j].String, valueToken.Boolean);
                                    break;
                                default:
                                    // 对于其他类型，尝试直接设置
                                    newDict.SetValue(keys[j].String, valueToken);
                                    break;
                            }
                        }
                    }

                    // 添加复制的字典
                    roundCopy.Add(newDict);
                }
                else
                {
                    // 其他类型直接添加
                    roundCopy.Add(token);
                }
            }
        }

        // 保存深拷贝的数据而不是引用
        roundAnswers[roundIndex] = roundCopy;

        // 序列化当前回合数据
        bool serializationSuccess = false;
        if (VRCJson.TrySerializeToJson(roundCopy, JsonExportType.Minify, out DataToken jsonToken))
        {
            serializedRoundAnswers[roundIndex] = jsonToken.String;
            serializationSuccess = true;
            Debug.Log($"[PinDataManager] 回合{roundIndex}数据序列化成功，长度:{jsonToken.String.Length}");
        }
        else
        {
            Debug.LogError($"[PinDataManager] 回合{roundIndex}数据序列化失败!");
        }

        // 只有序列化成功才继续
        if (serializationSuccess)
        {
            // 同步数据到所有客户端
            RequestSerialization();
            Debug.Log($"[PinDataManager] 已请求同步回合{roundIndex}数据");

            // 先更新本地UI
            UpdateGameRoundDataUI();

            // 然后通知所有其他客户端更新UI
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateGameRoundDataUI));
            Debug.Log($"[PinDataManager] 已发送UI更新事件给所有客户端");
        }
    }

    // 新增：更新游戏回合数据UI的方法（所有客户端都可调用）
    public void UpdateGameRoundDataUI()
    {
        if (gameDataStoreageText == null) return;

       
        StringBuilder storageText = new StringBuilder("Game Round Data:\n");
        for (int i = 0; i < totalRounds; i++)
        {
            storageText.AppendLine($"Round {i + 1}:");
            if (roundAnswers[i] != null && roundAnswers[i].Count > 0)  // 检查是否有数据
            {
                // 遍历该轮次的所有玩家答案
                for (int j = 0; j < roundAnswers[i].Count; j++)
                {
                    if (roundAnswers[i].TryGetValue(j, TokenType.DataDictionary, out DataToken dataToken))
                    {
                        DataDictionary playerAnswer = dataToken.DataDictionary;
                        if (playerAnswer.TryGetValue("id", out DataToken idToken) &&
                            playerAnswer.TryGetValue("longitude", out DataToken longToken) &&
                            playerAnswer.TryGetValue("latitude", out DataToken latToken))
                        {
                            // 将值转换为所需类型
                            int playerId = (int)Convert.ToDouble(idToken.ToString());
                            double latitude = Convert.ToDouble(latToken.ToString());
                            double longitude = Convert.ToDouble(longToken.ToString());

                            storageText.AppendLine($"Player {idToken}: Lat {latToken}, Long {longToken}");
                        }
                    }
                }
            }
            else
            {
                storageText.AppendLine("No data");
            }
            storageText.AppendLine();
        } 
        Debug.Log($"[PinDataManager] 更新游戏回合数据UI{storageText.ToString()}");
        gameDataStoreageText.text = storageText.ToString();
    }

    // PinDataManager.cs

    //public void CalculateFinalScores(GameManager gameManager)
    //{
    //    StringBuilder finalScores = new StringBuilder("Final Scores:\n");

    //    // 用 DataDictionary 存储玩家分数
    //    playerTotalScores = new DataDictionary();

    //    // 遍历每一轮
    //    for (int round = 0; round < totalRounds; round++)
    //    {
    //        // 检查这个回合是否已经完成（有没有存储玩家答案）
    //        if (roundAnswers[round] == null || roundAnswers[round].Count == 0)
    //        {
    //            Debug.LogWarning($"[PinDataManager] 回合 {round} 没有有效的玩家答案数据，跳过计分");
    //            continue;
    //        }

    //        // 获取该轮使用的图片索引
    //        int imageIndex = round < gameManager.roundImageIndices.Length ?
    //                         gameManager.roundImageIndices[round] :
    //                         gameManager.GetCurrentImageIndex();

    //        // 如果该回合没有记录图片索引或索引无效，使用默认索引(0)或跳过
    //        if (imageIndex < 0 || imageIndex >= gameManager.GetImageUrlsLength())
    //        {
    //            Debug.LogWarning($"[PinDataManager] 回合 {round} 的图片索引 {imageIndex} 无效，使用默认索引0");
    //            imageIndex = 0; // 使用第一张图片作为默认
    //        }

    //        // 获取对应图片的正确答案位置
    //        Vector2 correctAnswer = gameManager.locationData.GetLocationLatLong(imageIndex);
    //        Debug.Log($"[PinDataManager] 计算回合 {round} 的最终得分, 图片索引: {imageIndex}, 正确答案: {correctAnswer}");

    //        // 计算每个玩家在这轮的得分
    //        for (int i = 0; i < roundAnswers[round].Count; i++)
    //        {
    //            if (roundAnswers[round].TryGetValue(i, TokenType.DataDictionary, out DataToken dataToken))
    //            {
    //                DataDictionary playerAnswer = dataToken.DataDictionary;
    //                if (playerAnswer.TryGetValue("id", TokenType.Int, out DataToken idToken) &&
    //                    playerAnswer.TryGetValue("longitude", TokenType.Float, out DataToken longToken) &&
    //                    playerAnswer.TryGetValue("latitude", TokenType.Float, out DataToken latToken))
    //                {
    //                    int playerId = idToken.Int;
    //                    Vector2 playerGuess = new Vector2(latToken.Float, longToken.Float);

    //                    // 获取放置状态，如果不存在则默认为 false
    //                    bool isPlaced = false;
    //                    if (playerAnswer.TryGetValue("isPlaced", TokenType.Boolean, out DataToken placedToken))
    //                    {
    //                        isPlaced = placedToken.Boolean;
    //                    }

    //                    // 记录详细的调试信息
    //                    Debug.Log($"[PinDataManager] 回合 {round}, 玩家 {playerId} 猜测: {playerGuess}, 正确答案: {correctAnswer}, 已放置: {isPlaced}");

    //                    // 计算得分，考虑 pin 是否被放置
    //                    float score = CalculateScore(correctAnswer, playerGuess, isPlaced);

    //                    // 累加到玩家总分
    //                    float currentScore = 0;
    //                    string playerKey = $"player_{playerId}";

    //                    if (playerTotalScores.TryGetValue(playerKey, TokenType.Float, out DataToken currentScoreToken))
    //                    {
    //                        currentScore = currentScoreToken.Float;
    //                    }

    //                    playerTotalScores.SetValue(playerKey, currentScore + score);
    //                }
    //            }
    //        }
    //    }

    //    // 构建最终得分字符串
    //    DataList playerKeys = playerTotalScores.GetKeys();
    //    for (int i = 0; i < playerKeys.Count; i++)
    //    {
    //        string playerKey = playerKeys[i].String;
    //        if (playerTotalScores.TryGetValue(playerKey, TokenType.Float, out DataToken scoreToken))
    //        {
    //            int playerId = int.Parse(playerKey.Split('_')[1]);
    //            finalScores.AppendLine($"Player {playerId}: {scoreToken.Float:F0}");
    //        }
    //    }

    //    // 更新UI显示
    //    roundScoresText.text = finalScores.ToString();
    //}

    //public void CalculateFinalScores(GameManager gameManager)
    //{
    //    StringBuilder finalScores = new StringBuilder("Final Scores:\n");

    //    // 用 DataDictionary 存储玩家分数
    //    DataDictionary playerTotalScores = new DataDictionary();

    //    // 遍历每一轮
    //    for (int round = 0; round < totalRounds; round++)
    //    {
    //        // 获取该轮的正确答案
    //        Vector2 correctAnswer = gameManager.GetRoundAnswer(round);

    //        // 获取该轮的所有玩家答案
    //        if (roundAnswers[round] != null)
    //        {
    //            // 计算每个玩家在这轮的得分
    //            for (int i = 0; i < roundAnswers[round].Count; i++)
    //            {
    //                if (roundAnswers[round].TryGetValue(i, TokenType.DataDictionary, out DataToken dataToken))
    //                {
    //                    DataDictionary playerAnswer = dataToken.DataDictionary;
    //                    if (playerAnswer.TryGetValue("id", TokenType.Int, out DataToken idToken) &&
    //                        playerAnswer.TryGetValue("longitude", TokenType.Float, out DataToken longToken) &&
    //                        playerAnswer.TryGetValue("latitude", TokenType.Float, out DataToken latToken))
    //                    {
    //                        int playerId = idToken.Int;
    //                        Vector2 playerGuess = new Vector2(latToken.Float, longToken.Float);
    //                        //Vector2 playerGuess = new Vector2(longToken.Float, latToken.Float);

    //                        // 获取放置状态，如果不存在则默认为 false
    //                        bool isPlaced = false;
    //                        if (playerAnswer.TryGetValue("isPlaced", TokenType.Boolean, out DataToken placedToken))
    //                        {
    //                            isPlaced = placedToken.Boolean;
    //                        }

    //                        //Debug.Log($"[PinDataManager] Player {playerId} guess: {playerGuess},correctAnswer: {correctAnswer}, isPlaced: {isPlaced}");

    //                        // 计算得分，考虑 pin 是否被放置
    //                        float score = CalculateScore(correctAnswer, playerGuess, isPlaced);

    //                        // 累加到玩家总分
    //                        float currentScore = 0;
    //                        string playerKey = $"player_{playerId}";

    //                        if (playerTotalScores.TryGetValue(playerKey, TokenType.Float, out DataToken currentScoreToken))
    //                        {
    //                            currentScore = currentScoreToken.Float;
    //                        }

    //                        playerTotalScores.SetValue(playerKey, currentScore + score);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    // 构建最终得分字符串
    //    DataList playerKeys = playerTotalScores.GetKeys();
    //    for (int i = 0; i < playerKeys.Count; i++)
    //    {
    //        string playerKey = playerKeys[i].String;
    //        if (playerTotalScores.TryGetValue(playerKey, TokenType.Float, out DataToken scoreToken))
    //        {
    //            int playerId = int.Parse(playerKey.Split('_')[1]);
    //            finalScores.AppendLine($"Player {playerId}: {scoreToken.Float:F0}");
    //        }
    //    }

    //    // 更新UI显示
    //    roundScoresText.text = finalScores.ToString();
    //}

    //// 计算每一轮所有玩家的得分
    //public void UpdateScoresAndDisplayLeaderboard(GameManager gameManager, int roundIndex)
    //{
    //    StringBuilder roundScores = new StringBuilder("");

    //    // 获取图片索引（用于调试）
    //    int imageIndex = gameManager.roundImageIndices[roundIndex];

    //    // 获取该轮的正确答案
    //    Vector2 correctAnswer = gameManager.GetRoundAnswer(roundIndex);
    //    // 获取该轮的所有玩家答案
    //    if (roundAnswers[roundIndex] != null)
    //    {
    //        // 计算每个玩家在这轮的得分
    //        for (int i = 0; i < roundAnswers[roundIndex].Count; i++)
    //        {
    //            if (roundAnswers[roundIndex].TryGetValue(i, TokenType.DataDictionary, out DataToken dataToken))
    //            {
    //                DataDictionary playerAnswer = dataToken.DataDictionary;
    //                if (playerAnswer.TryGetValue("id", TokenType.Int, out DataToken idToken) &&
    //                    playerAnswer.TryGetValue("longitude", TokenType.Float, out DataToken longToken) &&
    //                    playerAnswer.TryGetValue("latitude", TokenType.Float, out DataToken latToken))
    //                {
    //                    int playerId = idToken.Int;
    //                    Vector2 playerGuess = new Vector2(latToken.Float, longToken.Float);
    //                    //Vector2 playerGuess = new Vector2(longToken.Float, latToken.Float);
    //                    // 获取放置状态，如果不存在则默认为 false
    //                    bool isPlaced = false;
    //                    if (playerAnswer.TryGetValue("isPlaced", TokenType.Boolean, out DataToken placedToken))
    //                    {
    //                        isPlaced = placedToken.Boolean;
    //                    }


    //                    // 计算得分时考虑放置状态
    //                    float score = CalculateScore(correctAnswer, playerGuess, isPlaced);

    //                    //Debug.Log($"[PinDataManager] Player {playerId} guess: {playerGuess},correctAnswer: {correctAnswer}, isPlaced: {isPlaced}, score: {score}");
    //                    //// 添加到字符串
    //                    //roundScores.AppendLine($"Player {playerId}: {score:F0}{(!isPlaced ? " (Not placed)" : "")}");

    //                    // 累加到全局总分
    //                    string playerKey = $"player_{playerId}";
    //                    float currentTotal = 0f;
    //                    if (playerTotalScores.TryGetValue(playerKey, TokenType.Float, out DataToken scoreToken))
    //                    {
    //                        currentTotal = scoreToken.Float;
    //                    }
    //                    float newTotal = currentTotal + score;
    //                    playerTotalScores.SetValue(playerKey, newTotal);
    //                }
    //            }
    //        }
    //    }

    //    // 添加：序列化玩家分数
    //    SerializePlayerScores();

    //    // 保存计算结果到同步变量
    //    currentRoundScoresText = GetSortedScoreText();

    //    // 立即更新本地UI
    //    UpdateRoundScoresUI();

    //    // 请求同步，这会触发其他客户端的 OnDeserialization
    //    RequestSerialization();

    //    // 发送网络事件，确保所有客户端都更新 UI         
    //    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateRoundScoresUI));
    //}

    // 计算每一轮所有玩家的得分
    public void UpdateScoresAndDisplayLeaderboard(GameManager gameManager, int roundIndex)
    {
        StringBuilder roundScores = new StringBuilder("");

        // 获取图片索引（用于调试）
        int imageIndex = gameManager.roundImageIndices[roundIndex];

        // 获取该轮的正确答案
        Vector2 correctAnswer = gameManager.GetRoundAnswer(roundIndex);

        // 获取该轮的所有玩家答案
        if (roundAnswers[roundIndex] != null)
        {
            // 计算每个玩家在这轮的得分
            for (int i = 0; i < roundAnswers[roundIndex].Count; i++)
            {
                if (roundAnswers[roundIndex].TryGetValue(i, TokenType.DataDictionary, out DataToken dataToken))
                {
                    DataDictionary playerAnswer = dataToken.DataDictionary;

                    // 不指定类型，灵活获取值
                    if (playerAnswer.TryGetValue("id", out DataToken idToken) &&
                        playerAnswer.TryGetValue("longitude", out DataToken longToken) &&
                        playerAnswer.TryGetValue("latitude", out DataToken latToken))
                    {
                        // 灵活处理id值
                        int playerId;
                        if (idToken.TokenType == TokenType.Int)
                            playerId = idToken.Int;
                        else if (idToken.TokenType == TokenType.Double)
                            playerId = (int)idToken.Double;
                        else
                            playerId = int.Parse(idToken.ToString());

                        // 灵活处理经纬度值
                        float latitude, longitude;

                        if (latToken.TokenType == TokenType.Float)
                            latitude = latToken.Float;
                        else if (latToken.TokenType == TokenType.Double)
                            latitude = (float)latToken.Double;
                        else
                            latitude = float.Parse(latToken.ToString());

                        if (longToken.TokenType == TokenType.Float)
                            longitude = longToken.Float;
                        else if (longToken.TokenType == TokenType.Double)
                            longitude = (float)longToken.Double;
                        else
                            longitude = float.Parse(longToken.ToString());

                        Vector2 playerGuess = new Vector2(latitude, longitude);

                        // 灵活获取放置状态
                        bool isPlaced = false;
                        if (playerAnswer.TryGetValue("isPlaced", out DataToken placedToken))
                        {
                            if (placedToken.TokenType == TokenType.Boolean)
                                isPlaced = placedToken.Boolean;
                            else
                                isPlaced = bool.Parse(placedToken.ToString());
                        }

                        // 计算得分时考虑放置状态
                        float score = CalculateScore(correctAnswer, playerGuess, isPlaced);

                        // Debug.Log($"[PinDataManager] Player {playerId} guess: {playerGuess}, correctAnswer: {correctAnswer}, isPlaced: {isPlaced}, score: {score}");

                        // 累加到全局总分 - 灵活获取当前分数
                        string playerKey = $"player_{playerId}";
                        float currentTotal = 0f;

                        if (playerTotalScores.TryGetValue(playerKey, out DataToken scoreToken))
                        {
                            if (scoreToken.TokenType == TokenType.Float)
                                currentTotal = scoreToken.Float;
                            else if (scoreToken.TokenType == TokenType.Double)
                                currentTotal = (float)scoreToken.Double;
                            else if (scoreToken.TokenType == TokenType.Int)
                                currentTotal = scoreToken.Int;
                            else
                                float.TryParse(scoreToken.ToString(), out currentTotal);
                        }

                        float newTotal = currentTotal + score;
                        playerTotalScores.SetValue(playerKey, newTotal);
                    }
                }
            }
        }

        // 添加：序列化玩家分数
        SerializePlayerScores();

        // 保存计算结果到同步变量
        currentRoundScoresText = GetSortedScoreText();

        // 立即更新本地UI
        UpdateRoundScoresUI();

        // 请求同步，这会触发其他客户端的 OnDeserialization
        RequestSerialization();

        // 发送网络事件，确保所有客户端都更新 UI         
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateRoundScoresUI));
    }

    // 添加这个方法来序列化玩家分数
    private void SerializePlayerScores()
    {
        if (Networking.IsOwner(gameObject) && playerTotalScores != null)
        {
            if (VRCJson.TrySerializeToJson(playerTotalScores, JsonExportType.Minify, out DataToken jsonToken))
            {
                serializedPlayerScores = jsonToken.String;
                RequestSerialization();
            }
            else
            {
                Debug.LogError("[PinDataManager] 无法序列化玩家分数");
            }
        }
    }


    public string GetSortedScoreText()
    {
        StringBuilder sortedScoreText = new StringBuilder("");

        DataList keys = playerTotalScores.GetKeys();
        int count = keys.Count;
        if (count == 0) return "No scores yet.";

        string[] keyArray = new string[count];
        float[] scoreArray = new float[count];

        for (int i = 0; i < count; i++)
        {
            string key = keys[i].String;
            keyArray[i] = key;

            if (playerTotalScores.TryGetValue(key, TokenType.Float, out DataToken token))
                scoreArray[i] = token.Float;
            else
                scoreArray[i] = 0f;
        }

        // 选择排序：从高到低
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (scoreArray[j] > scoreArray[i])
                {
                    float tempScore = scoreArray[i];
                    scoreArray[i] = scoreArray[j];
                    scoreArray[j] = tempScore;

                    string tempKey = keyArray[i];
                    keyArray[i] = keyArray[j];
                    keyArray[j] = tempKey;
                }
            }
        }
        int numberNo = 0;
        for (int i = 0; i < count; i++)
        {
            int playerId = int.Parse(keyArray[i].Split('_')[1]);
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);

           
           
            if (player != null)
            {
                sortedScoreText.AppendLine($"{numberNo + 1}: {player.displayName}: {scoreArray[i]:F0}");
                numberNo++;
            }
            else
            {
                // 如果找不到玩家，就不用显示名字了
                //sortedScoreText.AppendLine($"{i + 1}. Player_{playerId}: {scoreArray[i]:F0}");
            }


            //string playerName = player != null ? player.displayName : $"Player_{playerId}";

            //sortedScoreText.AppendLine($"{i + 1}. {playerName}: {scoreArray[i]:F0}");
        }

        return sortedScoreText.ToString();
    }

    public void ResetPlayerTotalScores()
    {
        playerTotalScores = new DataDictionary(); // 清空
        serializedPlayerScores = "{}"; // 清空序列化数据

        // 创建新的空数组
        roundAnswers = new DataList[totalRounds];
        serializedRoundAnswers = new string[totalRounds];

        // 初始化每个元素
        for (int i = 0; i < totalRounds; i++)
        {
            roundAnswers[i] = new DataList();
            serializedRoundAnswers[i] = "";
        }

        RequestSerialization(); // 确保同步
    }

    // 添加一个 UI 更新方法，可以被网络事件调用
    public void UpdateRoundScoresUI()
    {
        if (roundScoresText != null)
        {
            roundScoresText.text = currentRoundScoresText;
        }
    }

    //// 处理所有权转移
    //public override void OnOwnershipTransferred(VRCPlayerApi player)
    //{
    //    Debug.Log($"[PinDataManager] 所有权转移给 {player.displayName}");

    //    // 如果我们是新房主，确保所有内容都正确初始化
    //    if (Networking.IsOwner(gameObject))
    //    {
    //        // 确保UI基于反序列化的数据更新
    //        UpdateRoundScoresUI();
    //    }
    //}

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        Debug.LogError($"[PinDataManager] 所有权转移给 {player.displayName}");

        // 如果我们是新房主，确保数据结构完整性
        if (Networking.IsOwner(gameObject))
        {
            // 使用安全版本保护现有数据
            //ValidateRoundDataAfterOwnershipChange();

            SyncUIAfterOwnershipChange();
            // 通知所有客户端更新UI
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SyncUIAfterOwnershipChange));
        }
    }
    // 新增：专门用于所有权转移后的安全恢复
    public void ValidateRoundDataAfterOwnershipChange()
    {
        if (!Networking.IsOwner(gameObject)) return;

        Debug.LogError("[PinDataManager] 执行所有权转移后的数据验证");

        // 1. 首先确保数组大小正确，但不重置现有数据
        if (roundAnswers == null || roundAnswers.Length != totalRounds)
        {
            // 创建新数组，保留现有数据
            DataList[] newRoundAnswers = new DataList[totalRounds];
            string[] newSerializedRoundAnswers = new string[totalRounds];

            // 复制所有可以保留的数据
            for (int i = 0; i < totalRounds; i++)
            {
                if (roundAnswers != null && i < roundAnswers.Length && roundAnswers[i] != null)
                {
                    newRoundAnswers[i] = roundAnswers[i];
                }
                else
                {
                    newRoundAnswers[i] = new DataList();
                }

                if (serializedRoundAnswers != null && i < serializedRoundAnswers.Length && !string.IsNullOrEmpty(serializedRoundAnswers[i]))
                {
                    newSerializedRoundAnswers[i] = serializedRoundAnswers[i];
                }
                else
                {
                    newSerializedRoundAnswers[i] = "";
                }
            }

            // 更新引用
            roundAnswers = newRoundAnswers;
            serializedRoundAnswers = newSerializedRoundAnswers;
        }

        // 2. 重新序列化所有有效数据（只在真正需要时重新序列化）
        bool needsSerialization = false;

        for (int i = 0; i < totalRounds; i++)
        {
            // 如果有数据但没有序列化字符串，重新序列化
            if (roundAnswers[i] != null && roundAnswers[i].Count > 0 &&
                (string.IsNullOrEmpty(serializedRoundAnswers[i]) || serializedRoundAnswers[i] == "[]"))
            {
                if (VRCJson.TrySerializeToJson(roundAnswers[i], JsonExportType.Minify, out DataToken jsonToken))
                {
                    serializedRoundAnswers[i] = jsonToken.String;
                    needsSerialization = true;
                    Debug.LogError($"[PinDataManager] 回合{i}数据重新序列化，长度:{jsonToken.String.Length}");
                }
            }
        }

        // 3. 如果有任何数据被重新序列化，请求同步
        if (needsSerialization)
        {
            RequestSerialization();
            Debug.LogError("[PinDataManager] 所有权转移后请求数据同步");
        }

        // 4. 强制更新所有UI
        UpdateGameRoundDataUI();
        UpdateRoundScoresUI();
    }

    // 新增：处理所有权变更后的UI同步
public void SyncUIAfterOwnershipChange()
{
    UpdateRoundScoresUI();
    UpdateGameRoundDataUI();
}

    private float CalculateScore(Vector2 correctAnswer, Vector2 playerGuess, bool isPlacedOnMap)
    {
        // If Pin isn't placed on the map, return 0 points
        if (!isPlacedOnMap)
        {
            return 0;
        }

        // Calculate distance (using Euclidean distance)
        float distance = Vector2.Distance(correctAnswer, playerGuess);

        //Debug.Log($"[PinDataManager] Distance: {distance}");

        // More sensitive score calculation
        float maxScore = 100f;
        float maxDistance = 15f; // Reduced from 1000 to make scoring more sensitive to distance

        // Optional: Add a minimum score threshold for very close guesses
        float minDistance = 1f; // Within 5 units is considered "very close"

        if (distance <= minDistance)
        {
            // Very close guesses get max or near-max points
            return maxScore * 0.95f + (minDistance - distance) / minDistance * (maxScore * 0.05f);
        }
        else if (distance >= maxDistance)
        {
            // Too far gets 0 points
            return 0;
        }
        else
        {
            // Use a non-linear curve to make score drop more quickly with distance
            // This creates more meaningful differentiation between close and far pins
            float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);
            return maxScore * 0.95f * (1 - Mathf.Pow(normalizedDistance, 2));
        }
    }

    private void UpdatePinVisibility()
    {

        if (!Networking.IsOwner(objectAssigner.gameObject))
        {
            Debug.LogWarning("[PinDataManager] 当前玩家不是 objectAssigner 的所有者，可能无法正确获取对象");
        }
        else
        {
            //Debug.Log("[PinDataManager] 当前玩家是 objectAssigner 的所有者");
        }



        // 获取所有活动的池对象
        Component[] activePoolObjects = objectAssigner._GetActivePoolObjects();
        // 显示Pool中的所有对象
        // 添加日志显示长度
        //Debug.Log($"[PinDataManager] 活动池对象数量: {activePoolObjects.Length}");

        if (activePoolObjects == null)
        {
            Debug.LogError("[PinDataManager] 无法获取活动的池对象！");
            return;
        }

        foreach (Component poolObject in activePoolObjects)
        {
            if (poolObject == null) continue;

            GameObject pinObject = poolObject.gameObject;
            VRCPlayerApi owner = Networking.GetOwner(pinObject);
            if (owner == null || !owner.IsValid()) continue;

            int pinOwnerId = owner.playerId;
            bool isOwner = (Networking.LocalPlayer.playerId == pinOwnerId);

            // 更新渲染器可见性
            Renderer[] renderers = pinObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = (showAllPins || isOwner) ? 1.0f : 0.0f;
                    mat.color = color;

                    mat.SetFloat("_Mode", (showAllPins || isOwner) ? 0 : 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", (showAllPins || isOwner) ? 1 : 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (showAllPins || isOwner) ? -1 : 3000;
                }
            }

            // 更新UI元素可见性
            CanvasGroup[] canvasGroups = pinObject.GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = (showAllPins || isOwner) ? 1.0f : 0.0f;
                canvasGroup.interactable = (showAllPins || isOwner);
                canvasGroup.blocksRaycasts = (showAllPins || isOwner);
            }
        }

        //Debug.Log($"[PinDataManager] 更新了 {activePoolObjects.Length} 个Pin的可见性, showAllPins: {showAllPins}");
    }

    // 在数据反序列化前调用
    public override void OnDeserialization()
    {

        //Debug.Log("OnDeserialization called");

   

        // 更新轮次得分 UI
        UpdateRoundScoresUI();

        //// 如果数组还没有初始化，则进行初始化
        //if (roundAnswers == null || serializedRoundAnswers == null ||
        //roundAnswers.Length != totalRounds || serializedRoundAnswers.Length != totalRounds)
        //{
        //    //Debug.Log("Initializing roundAnswers and serializedRoundAnswers arrays");
        //    roundAnswers = new DataList[totalRounds];
        //    serializedRoundAnswers = new string[totalRounds];
        //    for (int i = 0; i < totalRounds; i++)
        //    {
        //        roundAnswers[i] = new DataList();
        //        serializedRoundAnswers[i] = "";
        //    }
        //}

        // 首先检查数组是否已经初始化
        if (roundAnswers == null || serializedRoundAnswers == null)
        {
            Debug.Log("[PinDataManager] 数组未初始化，创建新数组");
            roundAnswers = new DataList[totalRounds];
            serializedRoundAnswers = new string[totalRounds];

            for (int i = 0; i < totalRounds; i++)
            {
                roundAnswers[i] = new DataList();
                serializedRoundAnswers[i] = "";
            }
            return; // 首次初始化直接返回，等待下一次同步
        }

        // 检查数组大小是否需要调整
        if (roundAnswers.Length != totalRounds || serializedRoundAnswers.Length != totalRounds)
        {
            Debug.LogWarning($"[PinDataManager] 数组大小不匹配 (当前: {roundAnswers.Length}, 期望: {totalRounds})，调整大小");

            // 创建新数组
            DataList[] newRoundAnswers = new DataList[totalRounds];
            string[] newSerializedRoundAnswers = new string[totalRounds];

            // 复制现有数据，确保不丢失
            for (int i = 0; i < totalRounds; i++)
            {
                if (i < roundAnswers.Length)
                {
                    newRoundAnswers[i] = roundAnswers[i];
                    if (i < serializedRoundAnswers.Length)
                    {
                        newSerializedRoundAnswers[i] = serializedRoundAnswers[i];
                    }
                    else
                    {
                        newSerializedRoundAnswers[i] = "";
                    }
                }
                else
                {
                    newRoundAnswers[i] = new DataList();
                    newSerializedRoundAnswers[i] = "";
                }
            }

            // 更新引用
            roundAnswers = newRoundAnswers;
            serializedRoundAnswers = newSerializedRoundAnswers;
        }

        if (!string.IsNullOrEmpty(serializedData))
        {
            if (VRCJson.TryDeserializeFromJson(serializedData, out DataToken dataToken) && dataToken.TokenType == TokenType.DataList)
            {
                dataList = dataToken.DataList;
            }
            else
            {
                Debug.LogError("Failed to deserialize JSON to DataList.");
                dataList = new DataList(); // 创建新的空DataList作为回退         
            }
        }
        else
        {
            Debug.LogError("Failed to deserialize JSON to DataList.02");
            // 如果serializedData为空或者是"[]"，则初始化为空的DataList
            dataList = new DataList();
        }

        // 这里更新 UI 确保所有玩家都能看到最新的信息
        dataListPlayerText.text = playerInfo;

        //// 反序列化每轮的数据
        //for (int i = 0; i < totalRounds; i++)
        //{
        //    if (!string.IsNullOrEmpty(serializedRoundAnswers[i]))
        //    {
        //        if (VRCJson.TryDeserializeFromJson(serializedRoundAnswers[i], out DataToken dataToken) &&
        //            dataToken.TokenType == TokenType.DataList)
        //        {
        //            roundAnswers[i] = dataToken.DataList;
        //        }
        //    }
        //}
        // 反序列化每一回合的数据
        for (int i = 0; i < totalRounds; i++)
        {
            //Debug.Log($"[PinDataManager] OnDeserialization - 访问索引 {i}，数组长度: {serializedRoundAnswers.Length}, totalRounds: {totalRounds}");

            if (i < serializedRoundAnswers.Length && !string.IsNullOrEmpty(serializedRoundAnswers[i]))
            {
                if (VRCJson.TryDeserializeFromJson(serializedRoundAnswers[i], out DataToken dataToken))
                {
                    if (dataToken.TokenType == TokenType.DataList)
                    {
                        roundAnswers[i] = dataToken.DataList;
                        //Debug.Log($"[PinDataManager] Successfully deserialized round {i} data");
                    }
                    else
                    {
                        Debug.LogError($"[PinDataManager] Round {i} data type error: {dataToken.TokenType}");
                    }
                }
                else
                {
                    Debug.LogError($"[PinDataManager] Failed to deserialize round {i} data");
                }
            }
        }

             // 添加这部分来反序列化玩家分数
        if (!string.IsNullOrEmpty(serializedPlayerScores))
        {
            if (VRCJson.TryDeserializeFromJson(serializedPlayerScores, out DataToken scoreToken) &&
                scoreToken.TokenType == TokenType.DataDictionary)
            {
                playerTotalScores = scoreToken.DataDictionary;
                Debug.Log($"[PinDataManager] 成功反序列化玩家分数{playerTotalScores}");
            }
            else
            {
                Debug.LogError("[PinDataManager] 无法反序列化玩家分数");
            }
        }

        UpdateGameRoundDataUI();
        //// 更新Pin可见性
        //UpdatePinVisibility();


    }

    public void ResetAllPinsToOrigin()
    {
        //Debug.Log("[PinDataManager] 正在归位所有Pin...");

        if (objectAssigner == null)
        {
            Debug.LogWarning("[PinDataManager] objectAssigner 未设置，无法归位Pins");
            return;
        }

        Component[] activePoolObjects = objectAssigner._GetActivePoolObjects();
        if (activePoolObjects == null)
        {
            Debug.LogWarning("[PinDataManager] 没有获取到活跃的Pool对象");
            return;
        }

        foreach (Component poolObject in activePoolObjects)
        {
            if (poolObject == null) continue;

            GameObject pin = poolObject.gameObject;
            if (resetTargetTransform != null)
            {
                pin.transform.position = resetTargetTransform.position;
                pin.transform.rotation = resetTargetTransform.rotation;
            }
            else
            {
                pin.transform.position = Vector3.zero;
                pin.transform.rotation = Quaternion.identity;
            }
        }

        //Debug.Log($"[PinDataManager] 已归位 {activePoolObjects.Length} 个Pin");
    }

    public void ResetAllPinsToOriginNetwork()
    {
        // 发送网络事件，确保所有客户端都归位
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetAllPinsToOrigin));
    }
    }
