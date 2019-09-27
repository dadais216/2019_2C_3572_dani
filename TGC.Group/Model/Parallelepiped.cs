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
        //estaba entre guardar los vertices o tener los 12 triangulos directamente. Me mande por los vertices porque
        //TGCTriangle parecia meter mucho bloat, ademas las transformaciones serian mas lentas. Usando los vertices
        //serian mas rapidas, pero seria mas lento la colision porque genero los triangulos en el momento. Espero
        //tener mas transformaciones que colisiones igual, ademas probablemente exista una colision mejor que 
        //probar con cada triangulo
        public TGCVector3[] vertex = new TGCVector3[8];
        public TGCVector3[] transformedVertex;


        /// <summary>
        ///     Crea una caja vacia
        /// </summary>
        public Parallelepiped()
        {
            //triangles = new TgcTriangle[12];
            transformedVertex = new TGCVector3[8];
        }


        

        public void transform(TGCMatrix transform)
        {

            for (int i = 0; i < 8; i++)
            {
                transformedVertex[i] = TGCVector3.TransformCoordinate(vertex[i], transform);
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

        public void updateValues(TGCVector3 size, TGCVector3 pos)
        {
            var x = pos.X;
            var y = pos.Y;
            var z = pos.Z;
            var sx = size.X / 2;
            var sy = size.Y / 2;
            var sz = size.Z / 2;

            vertex[0] = new TGCVector3(x - sx, y - sy, z - sz);
            vertex[1] = new TGCVector3(x + sx, y - sy, z - sz);
            vertex[2] = new TGCVector3(x - sx, y + sy, z - sz);
            vertex[3] = new TGCVector3(x + sx, y + sy, z - sz);
            vertex[4] = new TGCVector3(x - sx, y - sy, z + sz);
            vertex[5] = new TGCVector3(x + sx, y - sy, z + sz);
            vertex[6] = new TGCVector3(x - sx, y + sy, z + sz);
            vertex[7] = new TGCVector3(x + sx, y + sy, z + sz);

        }
        public static Parallelepiped fromBounding(TgcBoundingAxisAlignBox b)
        {
            var box = new Parallelepiped();

            var size = b.calculateSize();
            var pos = b.calculateBoxCenter();

            box.updateValues(size,pos);
            return box;
        }

        public static Parallelepiped fromSizePosition(TGCVector3 size, TGCVector3 pos)
        {
            var box = new Parallelepiped();
            box.updateValues(size,pos);
            return box;
        }


        #endregion Creacion
    }
}
