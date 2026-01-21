using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CircleArea : UdonSharpBehaviour
{
    public float radius = 5f;  // 圆的半径  
    public Material material;   // 材质  
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Start()
    {
        // 获取组件引用  
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogError("请确保GameObject上已添加MeshFilter和MeshRenderer组件！");
            return;
        }

        // 创建平面mesh  
        CreateCircleMesh();

        // 设置材质  
        if (material != null)
        {
            meshRenderer.material = material;
        }
    }

    void CreateCircleMesh()
    {
        Mesh mesh = new Mesh();

        // 创建平面顶点  
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-radius, 0, -radius);
        vertices[1] = new Vector3(radius, 0, -radius);
        vertices[2] = new Vector3(-radius, 0, radius);
        vertices[3] = new Vector3(radius, 0, radius);

        // 创建三角形索引  
        int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

        // 创建UV坐标  
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        uvs[3] = new Vector2(1, 1);

        // 设置mesh数据  
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    // 可选：添加公共方法用于在运行时更改圆形区域的大小  
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        CreateCircleMesh();
    }
}