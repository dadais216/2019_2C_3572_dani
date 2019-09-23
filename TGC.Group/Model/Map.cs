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

        GameModel game;

        public Chunks chunks=new Chunks();

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


        public Map(GameModel game_)
        {
            game = game_;
            Meshc.chunks = chunks;

            /*Shrub = GetMeshFromScene("Arbusto\\Arbusto-TgcScene.xml");
            Shrub2 = GetMeshFromScene("Arbusto2\\Arbusto2-TgcScene.xml");
            Plant = GetMeshFromScene("Planta\\Planta-TgcScene.xml");
            Plant2 = GetMeshFromScene("Planta2\\Planta2-TgcScene.xml");
            Plant3 = GetMeshFromScene("Planta3\\Planta3-TgcScene.xml");*/
        }

        //hay algun motivo para separar construccion y init?
        public void Init()
        {
            initSky();

            terrain = new TgcSimpleTerrain();
            terrain.loadHeightmap(game.MediaDir+"FBDV.jpg", xzTerrainScale, yTerrainScale,new TGCVector3(0, -yTerrainOffset, 0));
            //terrain.loadTexture(game.MediaDir + "caja.jpg");
            terrain.loadTexture(game.MediaDir + "TexturesCom_RoadsDirt0081_1_seamless_S.jpg");

            PopulateMeshes("Pino\\Pino-TgcScene.xml", 500, true);

        }
        public void Render(TGCMatrix matriz)
        {
            chunks.render(matriz);
            terrain.Render();
            sky.Render();
        }

        private TgcTexture GetTexture(string textureName)
        {
            return TgcTexture.createTexture(Device, game.MediaDir + textureName + ".jpg");
        }

        private TgcMesh GetMeshFromScene(string scenePath)
        {
            var loader = new TgcSceneLoader();
            var auxScene = loader.loadSceneFromFile(game.MediaDir + scenePath);
            return auxScene.Meshes[0];
        }

        private void PopulateMeshes(string filename, int maxElements, bool withColission)
        {
            var mesh = GetMeshFromScene(filename);

            for (var i = 0; i < maxElements; i++)
            {
                Meshc meshc = new Meshc();
                meshc.mesh = mesh;

                var squareRad = 40000;
                var pos = new TGCVector3(Random.Next(-squareRad, squareRad), 0, Random.Next(-squareRad, squareRad));


                var hm = terrain.HeightmapData;
                pos.Y = (hm[(int)(pos.X / xzTerrainScale) + hm.GetLength(0) / 2, (int)(pos.Z / xzTerrainScale) + hm.GetLength(1) / 2] - yTerrainOffset) * yTerrainScale - 200f;

                var scale = TGCVector3.One * Random.Next(20, 500);

                meshc.mesh.AutoTransformEnable = false;
                meshc.originalMesh = TGCMatrix.Scaling(scale) * TGCMatrix.Translation(pos);



                //colision no tiene que ser igual al mesh

                var scaleDiff = new TGCVector3(1f, 20f, 1f) * 7.5f;
                var posDiff = new TGCVector3(0, .5f*scaleDiff.Y, 0);

                meshc.meshToParalleliped = TGCMatrix.Scaling(scaleDiff) * TGCMatrix.Translation(posDiff);

                meshc.paralleliped = Parallelepiped.fromTransform(
                    meshc.meshToParalleliped * meshc.originalMesh
                    );

                chunks.addVertexFall(meshc);
            }
        }

        private void initSky()
        {
            //aumentar render distance
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(D3DDevice.Instance.FieldOfView, D3DDevice.Instance.AspectRatio,
D3DDevice.Instance.ZNearPlaneDistance, D3DDevice.Instance.ZFarPlaneDistance * 1000f).ToMatrix();


            sky = new TgcSkyBox();
            sky.Center = TGCVector3.Empty;
            sky.Size = new TGCVector3(100000, 100000, 100000);

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
