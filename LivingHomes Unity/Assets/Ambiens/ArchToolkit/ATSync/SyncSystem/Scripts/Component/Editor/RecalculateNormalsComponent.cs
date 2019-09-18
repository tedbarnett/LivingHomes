using UnityEngine;
using ambiens.utils.loader;

[ExecuteInEditMode]
public class RecalculateNormalsComponent : MonoBehaviour
{
    public Mesh mesh;

    public float angle;

    public MeshFilter meshFilter
    {
        get
        {
            if (_meshFilter == null)
                _meshFilter = gameObject.GetComponent<MeshFilter>();

            return _meshFilter;
        }
    }

    private MeshFilter _meshFilter;

    private void OnEnable()
    {
        if(meshFilter != null)
        {
            this.mesh = meshFilter.sharedMesh;
        }
    }

    public void RecalculateNormals(float angle)
    {
        if (this.mesh != null)
            this.mesh.RecalculateNormals(angle);
    }
}
