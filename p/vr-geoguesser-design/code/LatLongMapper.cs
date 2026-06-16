using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class LatLongMapper : UdonSharpBehaviour
{
    public Vector2 topRightLatLong = new Vector2(90f, 180f);  // 东北角（右上角）
    public Vector2 bottomLeftLatLong = new Vector2(-90f, -180f); // 西南角（左下角）
    public RectTransform mapRectTransform; // RawImage的RectTransform


    // 将UI坐标转换为经纬度
    public Vector2 UICoordsToLatLong(Vector2 position)
    {
        // 获取RawImage的尺寸
        Vector2 size = mapRectTransform.rect.size;
        Debug.Log($"size:{size}");

        // 归一化坐标到0到1范围
        float normalizedX = Mathf.Clamp01((position.x + size.x / 2) / size.x);
        float normalizedY = Mathf.Clamp01((position.y + size.y / 2) / size.y);

        // 计算经度和纬度
        float longitude = Mathf.Lerp(bottomLeftLatLong.y, topRightLatLong.y, normalizedX);
        float latitude = Mathf.Lerp(bottomLeftLatLong.x, topRightLatLong.x, normalizedY);

        return new Vector2(latitude, longitude);
    }

    // 将经纬度转换为UI坐标
    public Vector2 LatLongToUICoords(Vector2 latLong)
    {
        // 获取RawImage的尺寸
        Vector2 size = mapRectTransform.rect.size;

        // 归一化经纬度到0到1范围
        float normalizedX = Mathf.InverseLerp(bottomLeftLatLong.y, topRightLatLong.y, latLong.y);
        float normalizedY = Mathf.InverseLerp(bottomLeftLatLong.x, topRightLatLong.x, latLong.x);

        // 计算UI坐标
        float x = (normalizedX * size.x) - (size.x / 2);
        float y = (normalizedY * size.y) - (size.y / 2);

        return new Vector2(x, y);
    }



    void Start()
    {
        Debug.Log("[LatLongMapper] 初始化完成");
    }
    }
