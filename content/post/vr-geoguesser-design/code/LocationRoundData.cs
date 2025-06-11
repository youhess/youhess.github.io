using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

public class LocationRoundData : UdonSharpBehaviour
{
    [SerializeField]
    private TextAsset jsonFile;  // JSON文件引用

    private DataList locationDataList;  // 存储所有位置数据

    void Start()
    {
        LoadLocationData();
    }

    private void LoadLocationData()
    {
        if (jsonFile != null)
        {
            string json = jsonFile.text;
            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                if (result.TokenType == TokenType.DataList)
                {
                    locationDataList = result.DataList;
                    //Debug.Log($"Loaded {locationDataList.Count} locations");
                }
            }
        }
    }

    // 获取位置信息
    //public Vector2 GetLocationLatLong(int index)
    //{
    //    if (locationDataList != null && index < locationDataList.Count)
    //    {
    //        var locationData = locationDataList[index].DataDictionary;
    //        float latitude = 0f, longitude = 0f;

    //        if (locationData.TryGetValue("latitude", out DataToken latValue))
    //        {
    //            latitude = (float)latValue.Double;
    //        }
    //        if (locationData.TryGetValue("longitude", out DataToken longValue))
    //        {
    //            longitude = (float)longValue.Double;
    //        }

    //        return new Vector2(latitude, longitude);
    //    }
    //    return Vector2.zero;
    //}

    public Vector2 GetLocationLatLong(int index)
    {
        if (locationDataList == null)
        {
            Debug.LogError($"[LocationRoundData] locationDataList is null!");
            return Vector2.zero;
        }

        // 边界检查
        if (index < 0 || index >= locationDataList.Count)
        {
            Debug.LogError($"[LocationRoundData] Index {index} out of range! Count: {locationDataList.Count}");
            return Vector2.zero;
        }

        // 获取位置数据
        if (!locationDataList.TryGetValue(index, TokenType.DataDictionary, out DataToken dataToken))
        {
            Debug.LogError($"[LocationRoundData] Failed to get DataDictionary at index {index}");
            return Vector2.zero;
        }

        var locationData = dataToken.DataDictionary;
        float latitude = 0f, longitude = 0f;

        // 获取经纬度
        if (locationData.TryGetValue("latitude", out DataToken latValue))
        {
            if (latValue.TokenType == TokenType.Float || latValue.TokenType == TokenType.Double)
            {
                latitude = (float)latValue.Double;
            }
        }

        if (locationData.TryGetValue("longitude", out DataToken longValue))
        {
            if (longValue.TokenType == TokenType.Float || longValue.TokenType == TokenType.Double)
            {
                longitude = (float)longValue.Double;
            }
        }

        //Debug.Log($"[LocationRoundData] Got location at index {index}: ({latitude}, {longitude})");
        return new Vector2(latitude, longitude);
    }

    // 获取中文地点名称
    public string GetLocationCnName(int index)
    {
        if (locationDataList != null && index < locationDataList.Count)
        {
            var locationData = locationDataList[index].DataDictionary;
            if (locationData.TryGetValue("cn_name", out DataToken nameValue))
            {
                return nameValue.String;
            }
        }
        return "Unknown Location";
    }

    // 获取英文地点名称
    public string GetLocationEnName(int index)
    {
        if (locationDataList != null && index < locationDataList.Count)
        {
            var locationData = locationDataList[index].DataDictionary;
            if (locationData.TryGetValue("en_name", out DataToken nameValue))
            {
                return nameValue.String;
            }
        }
        return "Unknown Location";
    }

    // 获取图片URL
    public string GetImageUrl(int index)
    {
        if (locationDataList != null && index < locationDataList.Count)
        {
            var locationData = locationDataList[index].DataDictionary;
            if (locationData.TryGetValue("image_url", out DataToken urlValue))
            {
                return urlValue.String;
            }
        }
        return "";
    }

    // 获取当前加载的位置数量
    public int GetLocationCount()
    {
        return locationDataList != null ? locationDataList.Count : 0;
    }
}