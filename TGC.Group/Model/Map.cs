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

        internal static readonly TGCVector3 Origin = new TGCVector3(0, 20000, 0);
        internal static readonly TGCVector3 Up = TGCVector3.Up;
        internal static readonly TGCVector3 Down = -Up;
        internal static readonly TGCVector3 East = new TGCVector3(1, 0, 0);
        internal static readonly TGCVector3 West = -East;
        internal static readonly TGCVector3 North = new TGCVector3(0, 0, 1);
        internal static readonly TGCVector3 South = -North;

        GameModel game;

        public List<Parallelepiped> collisions;

        public List<TgcMesh> scene = new List<TgcMesh>();

        private readonly TgcMesh Pine;
        /*
        private readonly TgcMesh Shrub;
        private readonly TgcMesh Shrub2;
        private readonly TgcMesh Plant;
        private readonly TgcMesh Plant2;
        private readonly TgcMesh Plant3;
        */
        private TgcSkyBox sky;

        public TgcSimpleTerrain terrain;
        public const float xzTerrainScale= 1000f;
        public const float yTerrainScale = 30f;


        public Map(GameModel game_)
        {
            game = game_;

            Pine = GetMeshFromScene("Pino\\Pino-TgcScene.xml");
            /*Shrub = GetMeshFromScene("Arbusto\\Arbusto-TgcScene.xml");
            Shrub2 = GetMeshFromScene("Arbusto2\\Arbusto2-TgcScene.xml");
            Plant = GetMeshFromScene("Planta\\Planta-TgcScene.xml");
            Plant2 = GetMeshFromScene("Planta2\\Planta2-TgcScene.xml");
            Plant3 = GetMeshFromScene("Planta3\\Planta3-TgcScene.xml");*/
        }

        //hay algun motivo para separar construccion y init?
        public void Init()
        {
            // Instancio cajas
            collisions = new List<Parallelepiped>();

            GenerateTexturedBox(new TGCVector3(-400f, 000f, -200f), TGCVector3.One * 600, "caja");
            GenerateTexturedBox(new TGCVector3(-00f, -1000f, -00f), TGCVector3.One * 600, "caja");

            for (var i = 0; i < 10; i++)
            {
                var size = Random.Next(20, 2000);
                var position = RandomPos() + Up * (size / 2);
                GenerateTexturedBox(position, TGCVector3.One * size, "caja");
            }



            var groundSize = (North + East) * 20000;
            var groundOrigin = -groundSize;
            groundOrigin.Multiply(0.5f);
            var plane = new TgcPlane(groundOrigin, groundSize, TgcPlane.Orientations.XZplane, GetTexture("pasto"));
            scene.Add(plane.toMesh("ground"));

            PopulateMeshes(Pine, 200, 2, 10, true);

            /*PopulateMeshes(Shrub2, 500, 1, 4, false);
            PopulateMeshes(Plant, 60, 1, 3, false);
            PopulateMeshes(Plant2, 60, 1, 3, false);
            PopulateMeshes(Plant3, 60, 1, 3, false);*/

            initSky();

            terrain = new TgcSimpleTerrain();
            terrain.loadHeightmap(game.MediaDir+"FBDV.jpg", xzTerrainScale, yTerrainScale,new TGCVector3(0,-1000,0));
            terrain.loadTexture(game.MediaDir + "caja.jpg");
        }
        public void Render(TGCMatrix matriz)
        {
            collisions[0].transform(matriz);
            foreach (var box in collisions)
            {
                box.renderAsPolygons();
            }
            //Scene.RenderAll();
            terrain.Render();
            sky.Render();
        }

        private int RanInt()
        {
            const int max = 3000;
            return Random.Next(-max, max);
        }

        private TGCVector3 RandomPos()
        {
            return RanInt() * North + RanInt() * East;
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

        private void PopulateMeshes(TgcMesh mesh, int maxElements, int scaleMin, int scaleMax, bool withColission)
        {
            if (scaleMin > scaleMax) return;
            for (var i = 0; i < maxElements; i++)
            {
                var newMesh = mesh.clone(i.ToString());
                newMesh.Position = RandomPos();
                newMesh.Scale = TGCVector3.One * Random.Next(scaleMin, scaleMax);
                newMesh.RotateY((float)(Math.PI * Random.NextDouble()));
                scene.Add(newMesh);
                if (withColission)
                {
                    newMesh.BoundingBox.scaleTranslate(newMesh.Position,
                        new TGCVector3(newMesh.Scale.X * 0.3f, newMesh.Scale.Y, newMesh.Scale.Z * 0.3f));
                    //Collisions.Add(newMesh.BoundingBox);
                }
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
D3DDevice.Instance.ZNearPlaneDistance, D3DDevice.Instance.ZFarPlaneDistance * 10f).ToMatrix();


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
