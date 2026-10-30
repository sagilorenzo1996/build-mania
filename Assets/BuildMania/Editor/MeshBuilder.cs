using System.Collections.Generic;
using UnityEngine;

namespace BuildMania.EditorTools
{
    /// <summary>
    /// Accumulates simple geometry into one mesh. Polygons are given clockwise as seen
    /// from outside (Unity's front-face winding) and are flat shaded; cylinders are smooth.
    /// </summary>
    sealed class MeshBuilder
    {
        readonly List<Vector3> vertices = new();
        readonly List<Vector3> normals = new();
        readonly List<int> triangles = new();

        /// <summary>Convex polygon, flat shaded.</summary>
        public void AddPolygon(params Vector3[] points)
        {
            var normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
            var start = vertices.Count;
            foreach (var point in points)
            {
                vertices.Add(point);
                normals.Add(normal);
            }
            for (var i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }

        /// <summary>Rectangle from <paramref name="origin"/> spanning <paramref name="up"/> and <paramref name="right"/> as seen from outside.</summary>
        public void AddFace(Vector3 origin, Vector3 up, Vector3 right) =>
            AddPolygon(origin, origin + up, origin + up + right, origin + right);

        public void AddBox(Vector3 min, Vector3 max)
        {
            var size = max - min;
            var dx = new Vector3(size.x, 0f, 0f);
            var dy = new Vector3(0f, size.y, 0f);
            var dz = new Vector3(0f, 0f, size.z);

            AddFace(min, dy, dx);                                    // front (-Z)
            AddFace(new Vector3(max.x, min.y, max.z), dy, -dx);      // back (+Z)
            AddFace(new Vector3(max.x, min.y, min.z), dy, dz);       // right (+X)
            AddFace(new Vector3(min.x, min.y, max.z), dy, -dz);      // left (-X)
            AddFace(new Vector3(min.x, max.y, min.z), dz, dx);       // top (+Y)
            AddFace(new Vector3(min.x, min.y, max.z), -dz, dx);      // bottom (-Y)
        }

        /// <summary>Smooth-sided cylinder from <paramref name="baseCentre"/> along <paramref name="axis"/>.</summary>
        public void AddCylinder(Vector3 baseCentre, Vector3 axis, float radius, float length, int segments, bool capStart, bool capEnd)
        {
            axis = axis.normalized;
            var u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var v = Vector3.Cross(u, axis);
            var endCentre = baseCentre + axis * length;

            var ring = new Vector3[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var angle = 2f * Mathf.PI * i / segments;
                ring[i] = Mathf.Cos(angle) * u + Mathf.Sin(angle) * v;
            }

            var sideStart = vertices.Count;
            for (var i = 0; i <= segments; i++)
            {
                vertices.Add(baseCentre + ring[i] * radius);
                normals.Add(ring[i]);
                vertices.Add(endCentre + ring[i] * radius);
                normals.Add(ring[i]);
            }
            for (var i = 0; i < segments; i++)
            {
                var bottom = sideStart + 2 * i;
                var top = bottom + 1;
                var nextBottom = bottom + 2;
                var nextTop = bottom + 3;
                triangles.Add(bottom);
                triangles.Add(top);
                triangles.Add(nextTop);
                triangles.Add(bottom);
                triangles.Add(nextTop);
                triangles.Add(nextBottom);
            }

            if (capEnd)
                AddCap(endCentre, axis, ring, radius, outward: true);
            if (capStart)
                AddCap(baseCentre, -axis, ring, radius, outward: false);
        }

        void AddCap(Vector3 centre, Vector3 normal, Vector3[] ring, float radius, bool outward)
        {
            var start = vertices.Count;
            vertices.Add(centre);
            normals.Add(normal);
            for (var i = 0; i < ring.Length; i++)
            {
                vertices.Add(centre + ring[i] * radius);
                normals.Add(normal);
            }
            for (var i = 0; i < ring.Length - 1; i++)
            {
                var current = start + 1 + i;
                triangles.Add(start);
                // The ring runs anticlockwise when viewed from the end cap, so flip the order there.
                triangles.Add(outward ? current + 1 : current);
                triangles.Add(outward ? current : current + 1);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
