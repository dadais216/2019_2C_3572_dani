using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using TGC.Core.BoundingVolumes;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using TGC.Group.Model.Camera;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        internal static readonly TGCVector3 Origin = TGCVector3.Empty;
        internal static readonly TGCVector3 Up = TGCVector3.Up;
        internal static readonly TGCVector3 Down = -Up;
        internal static readonly TGCVector3 East = new TGCVector3(1, 0, 0);
        internal static readonly TGCVector3 West = -East;
        internal static readonly TGCVector3 North = new TGCVector3(0, 0, 1);
        internal static readonly TGCVector3 South = -North;
        private readonly Device Device = D3DDevice.Instance.Device;
        private readonly Random Random = new Random();
        private readonly TgcScene Scene = new TgcScene("mapa", null);//por ahi cambiar con objeto propio, nomas necesito tener una lista de meshes

        private readonly List<TgcBoundingAxisAlignBox> Collisions = new List<TgcBoundingAxisAlignBox>();
        private readonly TgcMesh Pine;
        private readonly TgcMesh Shrub;
        private readonly TgcMesh Shrub2;
        private readonly TgcMesh Plant;
        private readonly TgcMesh Plant2;
        private readonly TgcMesh Plant3;
        private TgcSkyBox sky;

       



        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
            Pine = GetMeshFromScene("Pino\\Pino-TgcScene.xml");
            Shrub = GetMeshFromScene("Arbusto\\Arbusto-TgcScene.xml");
            Shrub2 = GetMeshFromScene("Arbusto2\\Arbusto2-TgcScene.xml");
            Plant = GetMeshFromScene("Planta\\Planta-TgcScene.xml");
            Plant2 = GetMeshFromScene("Planta2\\Planta2-TgcScene.xml");
            Plant3 = GetMeshFromScene("Planta3\\Planta3-TgcScene.xml");


        }




        

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {


            // Instancio cajas
            for (var i = 0; i < 10; i++)
            {
                var size = Random.Next(100, 300);
                var position = RandomPos() + Up * (size / 2);
                GenerateTexturedBox(position, TGCVector3.One * size, "caja");
            }
            var groundSize = (North + East) * 20000;
            var groundOrigin = -groundSize;
            groundOrigin.Multiply(0.5f);
            var plane = new TgcPlane(groundOrigin, groundSize, TgcPlane.Orientations.XZplane, GetTexture("pasto"));
            Scene.Meshes.Add(plane.toMesh("ground"));

            PopulateMeshes(Pine, 200, 2, 10,true);
            PopulateMeshes(Shrub, 500, 1, 4,false);
            PopulateMeshes(Shrub2, 500, 1, 4, false);
            PopulateMeshes(Plant, 60, 1, 3, false);
            PopulateMeshes(Plant2, 60, 1, 3, false);
            PopulateMeshes(Plant3, 60, 1, 3, false);

            initSky();

            // Instancio camara
            Camara = new Camera.Camera(Input, Collisions);

            
        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();




            PostUpdate();
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            PreRender();
            Scene.RenderAll();
            sky.Render();
            PostRender();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            Scene.DisposeAll();
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
            return TgcTexture.createTexture(Device, MediaDir + textureName + ".jpg");
        }

        private TgcMesh GetMeshFromScene(string scenePath)
        {
            var loader = new TgcSceneLoader();
            var auxScene = loader.loadSceneFromFile(MediaDir + scenePath);
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
                Scene.Meshes.Add(newMesh);
                if (withColission)
                {
                    newMesh.BoundingBox.scaleTranslate(newMesh.Position,
                        new TGCVector3(newMesh.Scale.X * 0.3f, newMesh.Scale.Y, newMesh.Scale.Z * 0.3f));
                    Collisions.Add(newMesh.BoundingBox);
                }
            }
        }

        private int boxNumber;
        private void GenerateTexturedBox(TGCVector3 position, TGCVector3 size, string textureName)
        {
            var box = TGCBox.fromSize(position, size, GetTexture(textureName));
            box.Transform = TGCMatrix.Translation(position);
            Scene.Meshes.Add(box.ToMesh("box" + boxNumber++));
            Collisions.Add(box.BoundingBox);
        }

        private void initSky()
        {
            //aumentar render distance
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(D3DDevice.Instance.FieldOfView, D3DDevice.Instance.AspectRatio,
D3DDevice.Instance.ZNearPlaneDistance, D3DDevice.Instance.ZFarPlaneDistance * 9f).ToMatrix();
            

            sky = new TgcSkyBox();
            sky.Center = TGCVector3.Empty;
            sky.Size = new TGCVector3(100000, 100000, 100000);

            //sky.Color = Color.OrangeRed;

            var texturesPath = MediaDir + "SkyBox\\";

            
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