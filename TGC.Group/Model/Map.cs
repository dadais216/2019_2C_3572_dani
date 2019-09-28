using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Core.Textures;

namespace TGC.Group.Model
{
    public class Map
    {
        private readonly Device Device = D3DDevice.Instance.Device;
        private readonly Random Random = new Random();

        internal static readonly TGCVector3 Origin = new TGCVector3(0, 0000, 0);
        internal static readonly TGCVector3 Up = TGCVector3.Up;
        internal static readonly TGCVector3 Down = -Up;
        internal static readonly TGCVector3 East = new TGCVector3(1, 0, 0);
        internal static readonly TGCVector3 West = -East;
        internal static readonly TGCVector3 North = new TGCVector3(0, 0, 1);
        internal static readonly TGCVector3 South = -North;

        GameModel game;//deberia hacer varios de estos objetos estaticos para no tener que hacer estas giladas

        public Chunks chunks;

        /*
        private readonly TgcMesh Shrub;
        private readonly TgcMesh Shrub2;
        private readonly TgcMesh Plant;
        private readonly TgcMesh Plant2;
        private readonly TgcMesh Plant3;
        */
        private TgcSkyBox sky;

        public TgcSimpleTerrain terrain;
        public const float xzTerrainScale= 2000f;
        public const float yTerrainScale = 40f;
        public const float yTerrainOffset = 300f;

        public const int treesPerChunk=1;


        public Map(GameModel game_)
        {
            game = game_;

            /*Shrub = GetMeshFromScene("Arbusto\\Arbusto-TgcScene.xml");
            Shrub2 = GetMeshFromScene("Arbusto2\\Arbusto2-TgcScene.xml");
            Plant = GetMeshFromScene("Planta\\Planta-TgcScene.xml");
            Plant2 = GetMeshFromScene("Planta2\\Planta2-TgcScene.xml");
            Plant3 = GetMeshFromScene("Planta3\\Planta3-TgcScene.xml");*/
        }

        public void Init(Camera.Camera camera)
        {
            chunks = new Chunks(camera);
            Meshc.chunks = chunks;

            initSky();

            terrain = new TgcSimpleTerrain();
            terrain.loadHeightmap(game.MediaDir+"h.jpg", xzTerrainScale, yTerrainScale,new TGCVector3(0, -yTerrainOffset, 0));
            //terrain.loadTexture(game.MediaDir + "caja.jpg");
            terrain.loadTexture(game.MediaDir + "TexturesCom_RoadsDirt0081_1_seamless_S.jpg");


            GameModel.matriz = TGCMatrix.Identity;

            

            AddTrees();
            AddChurch();
        }
        public void Render()
        {
            chunks.render();
            terrain.Render();
            sky.Render();


            //foreach(var mesh in scene.Meshes)
            //{
            //    mesh.UpdateMeshTransform();
            //    mesh.Transform = GameModel.matriz * mesh.Transform;
            //    mesh.Render();
            //    var p = Parallelepiped.fromBounding(mesh.BoundingBox);
            //    p.transform(mesh.Transform);
            //    p.renderAsPolygons();
            //}


        }

        private TgcMesh GetMeshFromScene(string scenePath)
        {
            var loader = new TgcSceneLoader();
            var auxScene = loader.loadSceneFromFile(game.MediaDir + scenePath);
            var ret= auxScene.Meshes[0];
            ret.AutoTransformEnable = false;
            return ret;
        }

        private void AddTrees()
        {
            var mesh = GetMeshFromScene("Pino\\Pino-TgcScene.xml");


            for (int j= 1;j < Chunks.chunksPerDim-1;j++)
            for(int k=1;k< Chunks.chunksPerDim-1;k++)
            {
                int mid = Chunks.chunksPerDim / 2;
                if (((k >= mid-5) && (k <= mid + 4)) && ((j >= mid-4) && (j <= mid + 4)))
                    continue;

                for (var i = 0; i < treesPerChunk; i++)
                {
                    Meshc meshc = new Meshc();
                    meshc.mesh = mesh;

                    int squareRad = (int)(Chunks.chunkLen/2f);
                    var pos = new TGCVector3(Random.Next(-squareRad, squareRad), 0, Random.Next(-squareRad, squareRad));
                    pos += chunks.chunks[j,k].center;

                    var hm = terrain.HeightmapData;
                    pos.Y = (hm[(int)(pos.X / xzTerrainScale) + hm.GetLength(0) / 2, (int)(pos.Z / xzTerrainScale) + hm.GetLength(1) / 2] - yTerrainOffset) * yTerrainScale - 200f;

                    var scale = TGCVector3.One * Random.Next(20, 500);

                    meshc.originalMesh = TGCMatrix.Scaling(scale) * TGCMatrix.Translation(pos);

                    var box = meshc.mesh.BoundingBox;
                    var size = box.calculateSize();
                    var posb = box.calculateBoxCenter();

                    size.X *= .1f;
                    size.Z *= .1f;

                    posb.X -= size.X * .2f;

                    meshc.paralleliped = Parallelepiped.fromSizePosition(
                            size,
                            posb
                        );

                    meshc.transformColission();

                    chunks.addVertexFall(meshc);
                }
            }
        }

        void AddChurch()
        {
            var loader = new TgcSceneLoader();
            var scene = loader.loadSceneFromFile(game.MediaDir + "church-TgcScene.xml");

            var mm = new MultiMeshc();

            mm.originalMesh = TGCMatrix.Translation(0, -160, 0)
                    * TGCMatrix.Scaling(75, 75, 75);

            int cantBoxes = 0;
            foreach (var m in scene.Meshes)
            {
                if (m.Name.StartsWith("Box"))
                {
                    cantBoxes++;
                }
            }
            const int putByHand = 7;
            mm.meshes = new TgcMesh[scene.Meshes.Count-cantBoxes];
            mm.parallelipeds = new Parallelepiped[cantBoxes+ putByHand];

            int meshIndex = 0;
            int parIndex = 0;
            foreach (var m in scene.Meshes)
            {
                if (m.Name.StartsWith("Box"))
                {
                    var par = Parallelepiped.fromBounding(m.BoundingBox);
                    mm.parallelipeds[parIndex++] = par;
                    par.transform(mm.originalMesh);
                    chunks.addVertexFall(par, mm);
                }
                else
                {
                    m.AutoTransformEnable = false;
                    mm.meshes[meshIndex++]=m;
                }
            }

            //---
            //+--
            //-+-
            //++-
            //--+
            //+-+
            //-++
            //+++

            //techito
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    1, 472, -257,
                    66, 413, -257,
                    1, 430, -257,
                    32, 413, -257,
                    1, 472, -214,
                    66, 413, -214,
                    1, 430, -214,
                    32, 413, -214

                );
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    -62, 414, -257,
                    -29, 414, -257,
                    1, 469, -257,
                    1, 430, -257,
                    -62, 414, -213,
                    -29, 414, -213,
                    1, 469, -213,
                    1, 430, -213
                );

            //bordes
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    -93, 279, -257,
                    -58, 279, -257,
                    -58, 327, -257,
                    -58.5f, 327.5f, -257,
                    -93, 279, -213,
                    -58, 279, -213,
                    -58, 327, -213,
                    -58.5f, 327.5f, -213
                );
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    95, 279, -257,
                    69, 279, -257,
                    69, 317, -257,
                    69.5f, 317.5f, -257,
                    95, 279, -213,
                    69, 279, -213,
                    69, 317, -213,
                    69.5f, 317.5f, -213
                );

            //fondo
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    166, 230, 260,
                    -163, 230, 260,
                    0, 332, 260,
                    0.5f, 332.5f, 260,
                    166, 230, 239,
                    -163, 230, 239,
                    0, 332, 239,
                    0.5f, 332.5f, 239
                );


            //techo
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    172, 231, 264,
                    0, 341, 264,
                    172, 227, 264,
                    0, 227f, 264,
                    172, 231, -217,
                    0, 341, -217,
                    172, 227, -217,
                    0, 227f, -217
                );
            mm.parallelipeds[parIndex++] = Parallelepiped.fromVertex(
                    -172, 231, 264,
                    0, 341, 264,
                    -172, 227, 264,
                    0, 227f, 264,
                    -172, 231, -217,
                    0, 341, -217,
                    -172, 227, -217,
                    0, 227f, -217
                );

            for(int i = cantBoxes; i < cantBoxes + putByHand; i++)
            {
                var par = mm.parallelipeds[i];
                par.transform(mm.originalMesh);
                chunks.addVertexFall(par, mm);
            }
            //foreach(var mesh in scene.Meshes)
            //{
            //    mesh.UpdateMeshTransform();
            //    mesh.Transform = GameModel.matriz * mesh.Transform;
            //    mesh.Render();
            //    var p = Parallelepiped.fromBounding(mesh.BoundingBox);
            //    p.transform(mesh.Transform);
            //    p.renderAsPolygons();
            //}

        }

        private void initSky()
        {
            //aumentar render distance
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(D3DDevice.Instance.FieldOfView, D3DDevice.Instance.AspectRatio,
D3DDevice.Instance.ZNearPlaneDistance, D3DDevice.Instance.ZFarPlaneDistance * 1000f).ToMatrix();


            sky = new TgcSkyBox();
            sky.Center = TGCVector3.Empty;
            sky.Size = new TGCVector3(800000, 800000, 800000);

            //sky.Color = Color.OrangeRed;

            var texturesPath = game.MediaDir + "SkyBox\\";


            sky.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "Up.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "Down.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "Left.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "Right.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "Front.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "Back.jpg");
            sky.SkyEpsilon = 25f;

            sky.Init();

        }
    }
}
