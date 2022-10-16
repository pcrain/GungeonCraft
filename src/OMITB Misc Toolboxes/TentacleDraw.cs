using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
	public class TentacleDraw : MonoBehaviour
	{
		public void Initialize(Transform t1, Transform t2)
		{
			this.Attach1 = t1;
			this.Attach2 = t2;
			this.m_mesh = new Mesh();
			this.m_vertices = new Vector3[20];
			this.m_mesh.vertices = this.m_vertices;
			int[] array = new int[54];
			Vector2[] uv = new Vector2[20];
			int num = 0;
			for (int i = 0; i < 9; i++)
			{
				array[i * 6] = num;
				array[i * 6 + 1] = num + 2;
				array[i * 6 + 2] = num + 1;
				array[i * 6 + 3] = num + 2;
				array[i * 6 + 4] = num + 3;
				array[i * 6 + 5] = num + 1;
				num += 2;
			}
			this.m_mesh.triangles = array;
			this.m_mesh.uv = uv;
			GameObject gameObject = new GameObject("cableguy");
			this.m_stringFilter = gameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.material = (BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material);
			meshRenderer.material.SetColor("_OverrideColor", new Color32(166, 10, 10, 255));
			this.m_stringFilter.mesh = this.m_mesh;
		}

		private void LateUpdate()
		{
			if (this.Attach1 && this.Attach2)
			{
				Vector3 v = this.Attach1.position.XY().ToVector3ZisY(-3f) + this.Attach1Offset.ToVector3ZisY(0f);
				Vector3 vector = this.Attach2.position.XY().ToVector3ZisY(-3f) + this.Attach2Offset.ToVector3ZisY(0f);
				this.BuildMeshAlongCurve(v, v, vector + new Vector3(0f, -2f, -2f), vector, 0.03125f);
				this.m_mesh.vertices = this.m_vertices;
				this.m_mesh.RecalculateBounds();
				this.m_mesh.RecalculateNormals();
			}
		}

		// Token: 0x0600694A RID: 26954 RVA: 0x00293170 File Offset: 0x00291370
		private void OnDestroy()
		{
			if (this.m_stringFilter)
			{
				UnityEngine.Object.Destroy(this.m_stringFilter.gameObject);
			}
		}

		// Token: 0x0600694B RID: 26955 RVA: 0x00293194 File Offset: 0x00291394
		private void BuildMeshAlongCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float meshWidth = 0.03125f)
		{
			Vector3[] vertices = this.m_vertices;
			Vector2? vector = null;
			for (int i = 0; i < 10; i++)
			{
				Vector2 vector2 = BraveMathCollege.CalculateBezierPoint((float)i / 9f, p0, p1, p2, p3);
				Vector2? vector3 = (i != 9) ? new Vector2?(BraveMathCollege.CalculateBezierPoint((float)i / 9f, p0, p1, p2, p3)) : null;
				Vector2 a = Vector2.zero;
				if (vector != null)
				{
					a += (Quaternion.Euler(0f, 0f, 90f) * (vector2 - vector.Value)).XY().normalized;
				}
				if (vector3 != null)
				{
					a += (Quaternion.Euler(0f, 0f, 90f) * (vector3.Value - vector2)).XY().normalized;
				}
				a = a.normalized;
				vertices[i * 2] = (vector2 + a * meshWidth).ToVector3ZisY(0f);
				vertices[i * 2 + 1] = (vector2 + -a * meshWidth).ToVector3ZisY(0f);
				vector = new Vector2?(vector2);
			}
		}

		// Token: 0x040065A0 RID: 26016
		public Transform Attach1;

		// Token: 0x040065A1 RID: 26017
		public Vector2 Attach1Offset;

		// Token: 0x040065A2 RID: 26018
		public Transform Attach2;

		// Token: 0x040065A3 RID: 26019
		public Vector2 Attach2Offset;

		// Token: 0x040065A4 RID: 26020
		private Mesh m_mesh;

		// Token: 0x040065A5 RID: 26021
		private Vector3[] m_vertices;

		// Token: 0x040065A6 RID: 26022
		private MeshFilter m_stringFilter;
	}
}
