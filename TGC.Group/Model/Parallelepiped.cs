using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX.Direct3D;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Textures;

namespace TGC.Group.Model
{
    //creo mi propia forma de colision porque necesito paralelipedos, TgcConvexPolyhedron no se banca transformaciones 
    //es un copy paste de box donde cambie las cosas que necesitaba
    public class Parallelepiped
    {
        private readonly VertexBuffer vertexBuffer;
        private readonly CustomVertex.PositionColoredTextured[] vertices;

        public TGCMatrix Transform { get; private set; }

        //estaba entre guardar los vertices o tener los 12 triangulos directamente. Me mande por los vertices porque
        //TGCTriangle parecia meter mucho bloat, ademas las transformaciones serian mas lentas. Usando los vertices
        //serian mas rapidas, pero seria mas lento la colision porque genero los triangulos en el momento. Espero
        //tener mas transformaciones que colisiones igual, ademas probablemente exista una colision mejor que 
        //probar con cada triangulo
        public TGCVector3[] vertex;
        private TGCVector3[] transformedVertex;


        //despues deberia sacar las cosas de dibujado de aca, dejar solo las colisiones y manejar lo demas por mesh

        /// <summary>
        ///     Crea una caja vacia
        /// </summary>
        public Parallelepiped()
        {
            vertices = new CustomVertex.PositionColoredTextured[36];
            vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), vertices.Length,
                D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColoredTextured.Format, Pool.Default);

            Transform = TGCMatrix.Identity;

            //triangles = new TgcTriangle[12];
            vertex = new TGCVector3[8];
            transformedVertex = new TGCVector3[8];
        }


        /// <summary>
        ///     Liberar los recursos de la caja
        /// </summary>
        public void Dispose()
        {
            if (vertexBuffer != null && !vertexBuffer.Disposed)
            {
                vertexBuffer.Dispose();
            }
        }

        /// <summary>
        ///     Actualiza la caja en base a los valores configurados
        /// </summary>
        public void updateValues()
        {
            var c = Color.White.ToArgb();
            var x = 1f / 2;
            var y = 1f / 2;
            var z = 1f / 2;
            const float u = 1f;
            const float v = 1f;
            const float offsetU = 0f;
            const float offsetV = 0f;

            // Front face
            vertices[0] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[1] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV + v);
            vertices[2] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU + u, offsetV);
            vertices[3] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV + v);
            vertices[4] = new CustomVertex.PositionColoredTextured(x, -y, z, c, offsetU + u, offsetV + v);
            vertices[5] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU + u, offsetV);

            // Back face (remember this is facing *away* from the camera, so vertices should be clockwise order)
            vertices[6] = new CustomVertex.PositionColoredTextured(-x, y, -z, c, offsetU, offsetV);
            vertices[7] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV);
            vertices[8] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU, offsetV + v);
            vertices[9] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU, offsetV + v);
            vertices[10] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV);
            vertices[11] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);

            // Top face
            vertices[12] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[13] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV + v);
            vertices[14] = new CustomVertex.PositionColoredTextured(-x, y, -z, c, offsetU, offsetV + v);
            vertices[15] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[16] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU + u, offsetV);
            vertices[17] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV + v);

            // Bottom face (remember this is facing *away* from the camera, so vertices should be clockwise order)
            vertices[18] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV);
            vertices[19] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU, offsetV + v);
            vertices[20] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[21] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV);
            vertices[22] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[23] = new CustomVertex.PositionColoredTextured(x, -y, z, c, offsetU + u, offsetV);

            // Left face
            vertices[24] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);
            vertices[25] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[26] = new CustomVertex.PositionColoredTextured(-x, -y, z, c, offsetU, offsetV + v);
            vertices[27] = new CustomVertex.PositionColoredTextured(-x, y, -z, c, offsetU + u, offsetV);
            vertices[28] = new CustomVertex.PositionColoredTextured(-x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[29] = new CustomVertex.PositionColoredTextured(-x, y, z, c, offsetU, offsetV);

            // Right face (remember this is facing *away* from the camera, so vertices should be clockwise order)
            vertices[30] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU, offsetV);
            vertices[31] = new CustomVertex.PositionColoredTextured(x, -y, z, c, offsetU, offsetV + v);
            vertices[32] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);
            vertices[33] = new CustomVertex.PositionColoredTextured(x, y, -z, c, offsetU + u, offsetV);
            vertices[34] = new CustomVertex.PositionColoredTextured(x, y, z, c, offsetU, offsetV);
            vertices[35] = new CustomVertex.PositionColoredTextured(x, -y, -z, c, offsetU + u, offsetV + v);

            vertexBuffer.SetData(vertices, 0, LockFlags.None);

            vertex[0] = new TGCVector3(-x, -y, -z);
            vertex[1] = new TGCVector3(x, -y, -z);
            vertex[2] = new TGCVector3(-x, y, -z);
            vertex[3] = new TGCVector3(x, y, -z);
            vertex[4] = new TGCVector3(-x, -y, z);
            vertex[5] = new TGCVector3(x, -y, z);
            vertex[6] = new TGCVector3(-x, y, z);
            vertex[7] = new TGCVector3(x, y, z);

        }

        public void transform(TGCMatrix transform)
        {
            Transform = transform;

            for (int i = 0; i < 8; i++)
            {
                transformedVertex[i] = TGCVector3.TransformCoordinate(vertex[i],Transform);
            }
        }

        public bool intersectRay(TgcRay ray, out float dist, out TGCVector3 q)
        {
            float t;
            dist = 9000;
            var face=new TGCVector3[4];
           
            face[0] = transformedVertex[0];
            face[1] = transformedVertex[2];
            face[2] = transformedVertex[3];
            face[3] = transformedVertex[1];
            if(TgcCollisionUtils.intersectRayConvexPolygon(ray, face, out t, out q))
                dist=Math.Min(t,dist);

            face[0] = transformedVertex[2];
            face[1] = transformedVertex[6];
            face[2] = transformedVertex[4];
            face[3] = transformedVertex[0];
            if (TgcCollisionUtils.intersectRayConvexPolygon(ray, face, out t, out q))
                dist = Math.Min(t, dist);

            face[0] = transformedVertex[6];
            face[1] = transformedVertex[7];
            face[2] = transformedVertex[5];
            face[3] = transformedVertex[4];
            if (TgcCollisionUtils.intersectRayConvexPolygon(ray, face, out t, out q))
                dist = Math.Min(t, dist);

            face[0] = transformedVertex[7];
            face[1] = transformedVertex[5];
            face[2] = transformedVertex[1];
            face[3] = transformedVertex[3];
            if (TgcCollisionUtils.intersectRayConvexPolygon(ray, face, out t, out q))
                dist = Math.Min(t, dist);

            face[0] = transformedVertex[6];
            face[1] = transformedVertex[7];
            face[2] = transformedVertex[3];
            face[3] = transformedVertex[2];
            if (TgcCollisionUtils.intersectRayConvexPolygon(ray, face, out t, out q))
                dist = Math.Min(t, dist);
            
            //0 4 5 1
            face[0] = transformedVertex[0];
            face[1] = transformedVertex[4];
            face[2] = transformedVertex[5];
            face[3] = transformedVertex[1];
            if (TgcCollisionUtils.intersectRayConvexPolygon(ray, face, out t, out q))
                dist = Math.Min(t, dist);

            if (dist != 9000)
            {
                t = dist;
                return true;
            }
            return false;
        }

        public void renderAsPolygons()
        {
            var poly=new TgcConvexPolygon();
            var face = new TGCVector3[4];
            
            poly.Color = Color.DarkOrchid;
            face[0] = transformedVertex[0];
            face[1] = transformedVertex[2];
            face[2] = transformedVertex[3];
            face[3] = transformedVertex[1];
            poly.BoundingVertices = face;
            poly.updateValues();
            poly.Render();

            poly.Color = Color.Wheat;
            face[0] = transformedVertex[2];
            face[1] = transformedVertex[6];
            face[2] = transformedVertex[4];
            face[3] = transformedVertex[0];
            poly.BoundingVertices = face;
            poly.updateValues();
            poly.Render();

            poly.Color = Color.DodgerBlue;
            face[0] = transformedVertex[6];
            face[1] = transformedVertex[7];
            face[2] = transformedVertex[5];
            face[3] = transformedVertex[4];
            poly.BoundingVertices = face;
            poly.updateValues();
            poly.Render();

            poly.Color = Color.DarkGoldenrod;
            face[0] = transformedVertex[7];
            face[1] = transformedVertex[5];
            face[2] = transformedVertex[1];
            face[3] = transformedVertex[3];
            poly.BoundingVertices = face;
            poly.updateValues();
            poly.Render();
            
            poly.Color = Color.Sienna;
            face[0] = transformedVertex[6];
            face[1] = transformedVertex[7];
            face[2] = transformedVertex[3];
            face[3] = transformedVertex[2];
            poly.BoundingVertices = face;
            poly.updateValues();
            poly.Render();
            
            poly.Color = Color.Crimson;
            face[0] = transformedVertex[0];
            face[1] = transformedVertex[4];
            face[2] = transformedVertex[5];
            face[3] = transformedVertex[1];
            poly.BoundingVertices = face;
            poly.updateValues();
            poly.Render();
            
        }



        #region Creacion

        public static Parallelepiped fromTransform(TGCMatrix transform)
        {
            var box = new Parallelepiped();
            box.updateValues();
            box.transform(transform);
            return box;
        }


        #endregion Creacion
    }
}
