using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;

public static class DySkyTools
{
    const float PI = Mathf.PI;
    const float TwoPI = 2.0f * Mathf.PI;
    const float HalfPI = 0.5f * Mathf.PI;
    const float InvPI = 1.0f / PI;
    const float InvTwoPI = 1.0f / TwoPI;
    const float InvHalfPI = 1.0f / HalfPI;

    [MenuItem("Tools/DySky/Gen atan2 LUT", false, 301)]
    public static void GenAtan2LUT()
    {
        const int width = 512;
        const int height = 512;
        const float invW = 1.0f / width;
        const float invH = 1.0f / height;
        Texture2D tex = new Texture2D(width, height);
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++) {
                float x = i * invW * 2.0f - 1.0f;
                float z = j * invH * 2.0f - 1.0f;
                float phi = Mathf.Atan2(z, x);
                float phi01 = phi * InvPI * 0.5f + 0.5f;
                float y = i * invW;
                float theta = Mathf.Asin(y);
                float theta01 = theta * InvPI + 0.5f;
                float hemi_theta01 = theta * InvHalfPI;
                tex.SetPixel(i, height - j - 1, new Color(phi01, theta01, hemi_theta01));
            }
        tex.Apply();
        byte[] bytes = tex.EncodeToJPG();
        GameObject.DestroyImmediate(tex);
        string file = EditorUtility.SaveFilePanel("save file", "", "Sky_LUT_ATAN2", "jpg");
        if (!string.IsNullOrEmpty(file))
            File.WriteAllBytes(file, bytes);

        /*
				half phi = atan2(eyeRay.z, eyeRay.x);
				half theta = asin(eyeRay.y);
				half2 uvSphere = half2(phi * UNITY_INV_PI * 0.5 + 0.5, theta * UNITY_INV_PI + 0.5);
				half2 uvHemisphere = half2(uvSphere.x, theta * UNITY_INV_HALF_PI);
         */
    }

    struct Vertex
    {
        public Vector3 position;
        public Vector2 uv;
    }

    private static void SaveMeshToObj(string file, List<Vertex> verts, List<Vector3Int> tris, string groupName = "", bool append = false)
    {
        if (string.IsNullOrEmpty(file)) return;
        StringBuilder sb = new StringBuilder();
        if (append && File.Exists(file))
            sb.AppendLine(File.ReadAllText(file));
        if (!string.IsNullOrEmpty(groupName))
            sb.AppendLine(string.Format("g {0}", groupName));
        foreach (var vert in verts)
        {
            sb.AppendLine(string.Format("v {0} {1} {2}", -vert.position.x, vert.position.y, vert.position.z));
        }
        foreach (var vert in verts)
        {
            sb.AppendLine(string.Format("vn {0} {1} {2}", -vert.position.x, vert.position.y, vert.position.z));
        }
        foreach (var vert in verts)
        {
            sb.AppendLine(string.Format("vt {0} {1}", vert.uv.x, vert.uv.y));
        }
        foreach (var tri in tris)
        {
            sb.AppendLine(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", tri.x + 1, tri.z + 1, tri.y + 1));
        }
        File.WriteAllText(file, sb.ToString());
    }

    private static void CuttingMeshByRangeY(ref List<Vertex> verts, ref List<Vector3Int> tris, float minY, float maxY)
    {
        List<Vertex> newVerts = new List<Vertex>();
        List<Vector3Int> newTris = new List<Vector3Int>();
        int n = verts.Count;
        List<bool> reserved = new List<bool>(n);
        for (int i = 0; i < n; i++) reserved.Add(false);
        foreach (var tri in tris)
        {
            float y1 = verts[tri.x].position.y;
            float y2 = verts[tri.y].position.y;
            float y3 = verts[tri.z].position.y;
            if (y1 >= minY && y1 <= maxY || y2 >= minY && y2 <= maxY || y3 >= minY && y3 <= maxY)
            {
                reserved[tri.x] = true;
                reserved[tri.y] = true;
                reserved[tri.z] = true;
                newTris.Add(tri);
            }
        }
        List<int> oldMapToNewIdx = new List<int>(n);
        for (int i = 0; i < n; i++) oldMapToNewIdx.Add(-1);
        for (int i = 0; i < n; i++)
        {
            if (reserved[i])
            {
                oldMapToNewIdx[i] = newVerts.Count;
                newVerts.Add(verts[i]);
            }
        }
        for (int i = 0; i < newTris.Count; i++)
        {
            Vector3Int tri = newTris[i];
            tri.x = oldMapToNewIdx[tri.x];
            tri.y = oldMapToNewIdx[tri.y];
            tri.z = oldMapToNewIdx[tri.z];
            newTris[i] = tri;
        }
        verts = newVerts;
        tris = newTris;
    }

    [MenuItem("Tools/DySky/Gen UVSphere Model", false, 302)]
    public static void GenUVSphereModel()
    {
        const int phaseSplit = 80;
        const int thetaSplit = 80;
        const float deltaPhaseSplit = 1.0f / phaseSplit;
        const float deltaThetaSplit = 1.0f / thetaSplit;

        List<Vertex> verts = new List<Vertex>();
        verts.Add(new Vertex() { position = new Vector3(0,  1, 0), uv = new Vector2(0.5f, 1) });
        verts.Add(new Vertex() { position = new Vector3(0, -1, 0), uv = new Vector2(0.5f, 0) });
        for (int i = 0; i <= phaseSplit; i++)
        {
            float phase = i * deltaPhaseSplit * TwoPI;
            float sinPhase = Mathf.Sin(phase);
            float cosPhase = Mathf.Cos(phase);
            for (int j = 1; j < thetaSplit; j++)
            {
                float theta = j * deltaThetaSplit * PI;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);
                Vertex vert = new Vertex();
                vert.position = new Vector3(sinTheta * cosPhase, cosTheta, sinTheta * sinPhase);
                vert.uv = new Vector2(phase * InvTwoPI, 1.0f - theta * InvPI);
                verts.Add(vert);
            }
        }
        List<Vector3Int> tris = new List<Vector3Int>();
        int cntBodyVert = thetaSplit - 1;
        int cntBodyTri = thetaSplit - 2;
        int id1, id2, id3;
        for (int i = 0; i < phaseSplit; i++)
        {
            // top
            id1 = 2 + cntBodyVert * i;
            id2 = 0;
            id3 = 2 + cntBodyVert * (i + 1);
            tris.Add(new Vector3Int(id1, id2, id3));
            // bottom
            id1 = 2 + cntBodyVert * i + (cntBodyVert - 1);
            id2 = 2 + cntBodyVert * (i + 1) + (cntBodyVert - 1);
            id3 = 1;
            tris.Add(new Vector3Int(id1, id2, id3));
            // body
            for (int j = 0; j < cntBodyTri; j++)
            {
                // left top triangle
                id1 = 2 + cntBodyVert * i + j + 1;
                id2 = 2 + cntBodyVert * i + j;
                id3 = 2 + cntBodyVert * (i + 1) + j;
                tris.Add(new Vector3Int(id1, id2, id3));
                // right bottom triangle
                id1 = 2 + cntBodyVert * (i + 1) + j;
                id2 = 2 + cntBodyVert * (i + 1) + j + 1;
                id3 = 2 + cntBodyVert * i + j + 1;
                tris.Add(new Vector3Int(id1, id2, id3));
            }
        }
        string file = EditorUtility.SaveFilePanel("save file", "", "UVShpere", "obj");
        SaveMeshToObj(file, verts, tris);
    }

    class IcoSphereMesh
    {
        public List<Vertex> verts = new List<Vertex>();
        public List<Vector3Int> tris = new List<Vector3Int>();

        private Dictionary<Int64, int> middlePointIndexCache = new Dictionary<Int64, int>();

        public IcoSphereMesh(float rotZ = 0f)
        {
            // create 12 vertices of a icosahedron
            float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
            AddVert(-1, t, 0);
            AddVert(1, t, 0);
            AddVert(-1, -t, 0);
            AddVert(1, -t, 0);

            AddVert(0, -1, t);
            AddVert(0, 1, t);
            AddVert(0, -1, -t);
            AddVert(0, 1, -t);

            AddVert(t, 0, -1);
            AddVert(t, 0, 1);
            AddVert(-t, 0, -1);
            AddVert(-t, 0, 1);

            // 5 faces around point 0
            AddTri(0, 11, 5);
            AddTri(0, 5, 1);
            AddTri(0, 1, 7);
            AddTri(0, 7, 10);
            AddTri(0, 10, 11);

            // 5 adjacent faces 
            AddTri(1, 5, 9);
            AddTri(5, 11, 4);
            AddTri(11, 10, 2);
            AddTri(10, 7, 6);
            AddTri(7, 1, 8);

            // 5 faces around point 3
            AddTri(3, 9, 4);
            AddTri(3, 4, 2);
            AddTri(3, 2, 6);
            AddTri(3, 6, 8);
            AddTri(3, 8, 9);

            // 5 adjacent faces 
            AddTri(4, 9, 5);
            AddTri(2, 4, 11);
            AddTri(6, 2, 10);
            AddTri(8, 6, 7);
            AddTri(9, 8, 1);

            if (rotZ != 0)
            {
                Quaternion quat = Quaternion.Euler(0f, 0f, rotZ);
                for (int i = 0; i < verts.Count; i++)
                {
                    Vertex vert = verts[i];
                    vert.position = quat * vert.position;
                    verts[i] = vert;
                }
            }
        }

        public int AddVert(float x, float y, float z)
        {
            verts.Add(new Vertex() { position = new Vector3(x, y, z).normalized });
            return verts.Count - 1;
        }

        public void AddTri(int x, int y, int z)
        {
            tris.Add(new Vector3Int(x, y, z));
        }

        public void Subdivide()
        {
            List<Vector3Int> tris2 = new List<Vector3Int>();
            foreach (var tri in tris)
            {
                // replace triangle by 4 triangles
                int a = getMiddlePoint(tri.x, tri.y);
                int b = getMiddlePoint(tri.y, tri.z);
                int c = getMiddlePoint(tri.z, tri.x);

                tris2.Add(new Vector3Int(tri.x, a, c));
                tris2.Add(new Vector3Int(tri.y, b, a));
                tris2.Add(new Vector3Int(tri.z, c, b));
                tris2.Add(new Vector3Int(a, b, c));
            }
            tris = tris2;
        }

        private int getMiddlePoint(int p1, int p2)
        {
            // first check if we have it already
            bool firstIsSmaller = p1 < p2;
            Int64 smallerIndex = firstIsSmaller ? p1 : p2;
            Int64 greaterIndex = firstIsSmaller ? p2 : p1;
            Int64 key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (this.middlePointIndexCache.TryGetValue(key, out ret))
                return ret;

            // not in cache, calculate it
            var point1 = this.verts[p1].position;
            var point2 = this.verts[p2].position;

            // add vertex makes sure point is on unit sphere
            int i = AddVert((point1.x + point2.x) / 2.0f, (point1.y + point2.y) / 2.0f, (point1.z + point2.z) / 2.0f);

            // store it, return index
            this.middlePointIndexCache.Add(key, i);
            return i;
        }
    }

    [MenuItem("Tools/DySky/Gen IcoSphere Model", false, 303)]
    public static void GenIcoSphereModel()
    {
        string file = EditorUtility.SaveFilePanel("save file", "", "IcoShpere", "obj");
        if (string.IsNullOrEmpty(file)) return;
        const int LOD_MAX = 2;
        for (int lod = 0; lod <= LOD_MAX; lod++)
        {
            IcoSphereMesh mesh = new IcoSphereMesh(30f);
            for (int i = lod; i <= 3; i++)
                mesh.Subdivide();
            //SaveMeshToObj(file, mesh.verts, mesh.tris, "IcoSphereLod" + lod, lod > 0);
            CuttingMeshByRangeY(ref mesh.verts, ref mesh.tris, -0.4f, 0.9f);
            //CuttingMeshByRangeY(ref mesh.verts, ref mesh.tris, -0.5f, 1.0f);
            string fileName = Path.GetFileNameWithoutExtension(file);
            string newName = fileName + "Lod" + lod;
            SaveMeshToObj(file.Replace(fileName, newName), mesh.verts, mesh.tris, newName, false);
        }
    }

    [MenuItem("Tools/DySky/Gen Star Map", false, 304)]
    public static void GenStarMap()
    {
        string file = EditorUtility.SaveFilePanel("save file", "", "StarMap", "jpg");
        if (string.IsNullOrEmpty(file)) return;
        const int SIZE = 2048;
        const int UNIT = 128;
        const float LOW_I = 1.0f;
        Texture2D tex = new Texture2D(SIZE, SIZE, TextureFormat.RGB565, false);
        for (int i = 0; i < SIZE; ++i)
            for (int j = 0; j < SIZE; ++j)
                tex.SetPixel(i, j, Color.black);
        for (int i = 0; i < SIZE / UNIT; ++i)
            for (int j = 0; j < SIZE / UNIT; ++j)
            {
                int sz_lv = UnityEngine.Random.Range(0, 100) > 90 ? 2 : 1;
                float intensity = UnityEngine.Random.Range(LOW_I, 1.0f);
                intensity = Mathf.Clamp01(intensity * sz_lv);
                int rndL = i * UNIT + sz_lv;
                int rndR = i * UNIT + UNIT - sz_lv - 1;
                int rndU = j * UNIT + sz_lv;
                int rndB = j * UNIT + UNIT - sz_lv - 1;
                int rndX = UnityEngine.Random.Range(rndL, rndR + 1);
                int rndY = UnityEngine.Random.Range(rndU, rndB + 1);
                tex.SetPixel(rndX, rndY, Color.white * intensity);
            }
        tex.Apply();
        File.WriteAllBytes(file, tex.EncodeToJPG());
    }

}
