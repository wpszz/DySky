using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DySkyUtils
{
    static Mesh _FullscreenQuad;
    public static Mesh FullscreenQuad
    {
        get
        {
            if (_FullscreenQuad)
                return _FullscreenQuad;
            _FullscreenQuad = new Mesh { name = "FullscreenQuad" };
            /* 
             * 2  3
             * 0  1
             */
            _FullscreenQuad.vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0)
            };
            _FullscreenQuad.triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };
            _FullscreenQuad.UploadMeshData(true);
            return _FullscreenQuad;
        }
    }

    static Material _CopyMaterial;
    public static Material CopyMaterial
    {
        get
        {
            if (_CopyMaterial)
                return _CopyMaterial;
            _CopyMaterial = Resources.Load<Material>("DySkyCopyMaterial");
            return _CopyMaterial;
        }
    }

}

class PriorityQueue<T>
{
    IComparer<T> comparer;
    T[] heap;
    public int Count { get; private set; }
    public PriorityQueue() : this(null) { }
    public PriorityQueue(int capacity) : this(capacity, null) { }
    public PriorityQueue(IComparer<T> comparer) : this(16, comparer) { }
    public PriorityQueue(int capacity, IComparer<T> comparer)
    {
        this.comparer = (comparer == null) ? Comparer<T>.Default : comparer;
        this.heap = new T[capacity];
    }
    public void Push(T v)
    {
        if (Count >= heap.Length) System.Array.Resize(ref heap, Count * 2);
        heap[Count] = v;
        SiftUp(Count++);
    }
    public T Pop()
    {
        var v = Top();
        heap[0] = heap[--Count];
        if (Count > 0) SiftDown(0);
        return v;
    }
    public T Top()
    {
        if (Count > 0) return heap[0];
        throw new System.Exception("Queue is empty");
    }
    void SiftUp(int n)
    {
        var v = heap[n];
        for (var n2 = n / 2; n > 0 && comparer.Compare(v, heap[n2]) > 0; n = n2, n2 /= 2) heap[n] = heap[n2];
        heap[n] = v;
    }
    void SiftDown(int n)
    {
        var v = heap[n];
        for (var n2 = n * 2; n2 < Count; n = n2, n2 *= 2)
        {
            if (n2 + 1 < Count && comparer.Compare(heap[n2 + 1], heap[n2]) > 0) n2++;
            if (comparer.Compare(v, heap[n2]) >= 0) break;
            heap[n] = heap[n2];
        }
        heap[n] = v;
    }
}
