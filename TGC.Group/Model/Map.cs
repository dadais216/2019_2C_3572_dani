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

        public List<Parallelepiped> collisions=new List<Parallelepiped>();//se usa para las cajas porque no son mesh. Eventualmente lo voy a sacar

        public List<Meshc> scene = new List<Meshc>();

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

            /*Shrub = GetMeshFromScene("Arbusto\\Arbusto-TgcScene.xml");
            Shrub2 = GetMeshFromScene("Arbusto2\\Arbusto2-TgcScene.xml");
            Plant = GetMeshFromScene("Planta\\Planta-TgcScene.xml");
            Plant2 = GetMeshFromScene("Planta2\\Planta2-TgcScene.xml");
            Plant3 = GetMeshFromScene("Planta3\\Planta3-TgcScene.xml");*/
        }

        //hay algun motivo para separar construccion y init?
        public void Init()
        {
            GenerateTexturedBox(new TGCVector3(0f, 0f, -0f), TGCVector3.One * 600, "caja");
            for (var i = 0; i < 0; i++)
            {
                var size = Random.Next(20, 4000);
                var position = new TGCVector3(-900000, Random.Next(8500, 9500), Random.Next(500, 5000));
                GenerateTexturedBox(position, TGCVector3.One * size, "caja");
            }

            initSky();

            terrain = new TgcSimpleTerrain();
            terrain.loadHeightmap(game.MediaDir+"FBDV.jpg", xzTerrainScale, yTerrainScale,new TGCVector3(0, -yTerrainOffset, 0));
            terrain.loadTexture(game.MediaDir + "caja.jpg");

            PopulateMeshes("Pino\\Pino-TgcScene.xml", 200, true);

        }
        public void Render(TGCMatrix matriz)
        {
            collisions[0].transform(matriz);
            foreach (var box in collisions)
            {
                box.renderAsPolygons();
            }
            foreach(var mesh in scene)
            {
                mesh.transform(matriz);
                mesh.mesh.Render();
                //mesh.paralleliped.renderAsPolygons();
            }
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
            for (var i = 0; i < maxElements; i++)
            {
                Meshc mesh = new Meshc();
                mesh.mesh = GetMeshFromScene(filename);

                var squareRad = 40000;
                var pos = new TGCVector3(Random.Next(-squareRad, squareRad),0,Random.Next(-squareRad, squareRad));


                var hm = terrain.HeightmapData;
                pos.Y = (hm[(int)(pos.X / xzTerrainScale)+hm.GetLength(0)/2, (int)(pos.Z/ xzTerrainScale)+hm.GetLength(1)/2] - yTerrainOffset )* yTerrainScale - 200f;

                var scale = TGCVector3.One * Random.Next(20, 500);

                mesh.mesh.Position = pos;
                mesh.mesh.Scale = scale;


                scale.Y *= 20;
                scale *= 7.5f;

                pos.Y += .5f*scale.Y;
                //colision no tiene que ser igual al mesh


                mesh.paralleliped = Parallelepiped.fromSize(
                    pos,
                    scale
                    ) ;


                mesh.mesh.UpdateMeshTransform();
                mesh.mesh.AutoTransformEnable = false;


                mesh.setOriginals();

                scene.Add(mesh);
            }
        }

        private void GenerateTexturedBox(TGCVector3 position, TGCVector3 size, string textureName)
        {
            var box = Parallelepiped.fromSize(position, size, GetTexture(textureName));
            box.transform(TGCMatrix.Translation(position));
            collisions.Add(box);
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
