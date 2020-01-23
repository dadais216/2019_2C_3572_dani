﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Collision;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using static TGC.Group.Model.Chunks;

namespace TGC.Group.Model
{
    public class Map
    {
        private readonly Device Device = D3DDevice.Instance.Device;
        public readonly Random Random = new Random();

        internal static readonly TGCVector3 Origin = new TGCVector3(0, 0000, 0);
        internal static readonly TGCVector3 Up = TGCVector3.Up;
        internal static readonly TGCVector3 Down = -Up;
        internal static readonly TGCVector3 East = new TGCVector3(1, 0, 0);
        internal static readonly TGCVector3 West = -East;
        internal static readonly TGCVector3 North = new TGCVector3(0, 0, 1);
        internal static readonly TGCVector3 South = -North;

        /*
        private readonly TgcMesh Shrub;
        private readonly TgcMesh Shrub2;
        private readonly TgcMesh Plant;
        private readonly TgcMesh Plant2;
        private readonly TgcMesh Plant3;
        */
        private TgcSkyBox sky;

        public const float xzTerrainScale = 2000f;
        public const float yTerrainScale = 40f;
        public const float yTerrainOffset = 300f;

        public const int treesPerChunk = 1;

        public Mostro mostro;//no sé si tiene mucho sentido que este en map, no me preocupa mucho igual

        public class Shaders
        {
            public Effect comun;
            public Effect arbol;

            //intente usar templates pero parece que no se la bancan
            public void setValue(string s,float t)
            {
                comun.SetValue(s, t);
                arbol.SetValue(s, t);
            }
            public void setValue(string s, ColorValue t)
            {
                comun.SetValue(s, t);
                arbol.SetValue(s, t);
            }
            public void setValue(string s, float[] t)
            {
                comun.SetValue(s, t);
                arbol.SetValue(s, t);
            }

        }
        Shaders shader=new Shaders();

        public const int lightCount = 9;//cambiar en shader tambien
        public TGCVector3[] lightPosition = new TGCVector3[lightCount];
        public int lightIndex;
        public Map()
        {
            shader.comun = TGCShaders.Instance.LoadEffect(TGCShaders.Instance.CommonShadersPath + "punto de luz.fx");
            shader.arbol = TGCShaders.Instance.LoadEffect(TGCShaders.Instance.CommonShadersPath + "arbol.fx");

            shader.setValue("lightAttenuation", .2f);
            shader.setValue("lightColor", ColorValue.FromColor(Color.White));
            shader.setValue("materialAmbientColor", ColorValue.FromColor(Color.White));
            shader.setValue("materialDiffuseColor", ColorValue.FromColor(Color.White));


            g.chunks = new Chunks();

            initSky();

            var terrain = new Terrain();
            terrain.loadHeightmap(g.game.MediaDir + "h.jpg", xzTerrainScale, yTerrainScale, new TGCVector3(0, -yTerrainOffset, 0));
            //terrain.loadTexture(game.MediaDir + "caja.jpg");
            terrain.loadTexture(g.game.MediaDir + "TexturesCom_RoadsDirt0081_1_seamless_S.jpg");
            terrain.Effect = shader.comun;

            GameModel.matriz = TGCMatrix.Identity;

            for (int i = 0; i < lightCount; i++)
            {
                lightPosition[i] = TGCVector3.One*float.MaxValue;
            }


            g.terrain = terrain;
            addTrees();
            addChurch();
            //addCandles();

            mostro = new Mostro();

            g.map = this;

        }

        public void Render()
        {
            shader.setValue("materialEmissiveColor", ColorValue.FromColor(Color.FromArgb(0,candlesPlaced<5?0:(candlesPlaced-5),0,0)));

            shader.setValue("eyePosition", TGCVector3.Vector3ToFloat4Array(g.camera.eyePosition));
            shader.setValue("lightIntensityEye", 50f+(g.hands.state>0?
                                                                (g.hands.state == 1?
                                                                250f + Random.Next(-50, 50)
                                                                : 250f + Random.Next(-50, 50)+ 250f + Random.Next(-50, 50))
                                                     :0));

            for(int i = 0; i < lightCount; i++)
            {
                //lightPos del frame anterior, no deberian ser muy distintas y ahorro tener que recorrer los chunks 2 veces
                //para conseguir las luces actuales
                shader.setValue("lightPosition["+i.ToString()+"]", TGCVector3.Vector3ToFloat4Array(lightPosition[i]));
                shader.setValue("lightIntensity[" + i.ToString() + "]", 250f + Random.Next(-50, 50));
            }

            //foreach(var pos in lightPosition)
            //{
            //    Console.WriteLine(pos.X.ToString()+"  "+pos.Z.ToString());
            //}
            //Console.WriteLine("----");


            lightIndex = 0;

            sky.Render();
            if (renderCandlePlace)
                renderCandles();
            g.chunks.render();



            if (GameModel.debugColission)
            {
                g.chunks.fromCoordinates(g.camera.eyePosition).renderDebugColission();
                TGCVector3 ray = g.camera.horx.Origin + TGCVector3.Down * 200f;
                TGCVector3 end = ray + g.camera.horx.Direction * 200f;

                TgcLine.fromExtremes(ray, end).Render();

                ray = g.camera.horz.Origin + TGCVector3.Down * 200f;
                end = ray + g.camera.horz.Direction * 200f;

                TgcLine.fromExtremes(ray, end).Render();
            }

            g.terrain.Render();
        }

        static public TgcMesh GetMeshFromScene(string scenePath)
        {
            var loader = new TgcSceneLoader();
            var auxScene = loader.loadSceneFromFile(g.game.MediaDir + scenePath);
            var ret = auxScene.Meshes[0];
            ret.AutoTransformEnable = false;
            return ret;
        }

        private bool coordsOccupied(int j, int k)
        {
            int mid = Chunks.chunksPerDim / 2;
            return ((k >= mid - 5) && (k <= mid + 4)) && ((j >= mid - 4) && (j <= mid + 4));
        }
        private TGCVector3 genPosInChunk(int j, int k)
        {
            int squareRad = (int)(Chunks.chunkLen / 2f);
            var pos = new TGCVector3(Random.Next(-squareRad, squareRad), 0, Random.Next(-squareRad, squareRad));
            pos += g.chunks.chunks[j, k].center;

            var hm = g.terrain.HeightmapData;
            pos.Y = (hm[(int)(pos.X / xzTerrainScale) + hm.GetLength(0) / 2, (int)(pos.Z / xzTerrainScale) + hm.GetLength(1) / 2] - yTerrainOffset) * yTerrainScale - 200f;
            return pos;
        }

        private void addTrees()
        {
            var pino = GetMeshFromScene("Pino-TgcScene.xml");
            pino.Effect = shader.arbol;
            pino.Technique = "DIFFUSE_MAP";
            pino.AlphaBlendEnable = false;//no se porque lo tenia seteado en true 




            for (int j = 1; j < Chunks.chunksPerDim - 1; j++)
                for (int k = 1; k < Chunks.chunksPerDim - 1; k++)
                {
                    if (coordsOccupied(j, k))
                        continue;

                    for (var i = 0; i < treesPerChunk; i++)
                    {
                        Meshc meshc = new Meshc();
                        meshc.mesh = pino;

                        TGCVector3 pos = genPosInChunk(j, k);

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

                        //chunks.addVertexFall(meshc);
                    }
                }
        }



        void addChurch()
        {
            var loader = new TgcSceneLoader();
            var scene = loader.loadSceneFromFile(g.game.MediaDir + "church-TgcScene.xml");

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
            mm.meshes = new TgcMesh[scene.Meshes.Count - cantBoxes];
            mm.parallelipeds = new Parallelepiped[cantBoxes + putByHand];

            int meshIndex = 0;
            int parIndex = 0;
            foreach (var m in scene.Meshes)
            {
                if (m.Name.StartsWith("Box"))
                {
                    var par = Parallelepiped.fromBounding(m.BoundingBox);
                    mm.parallelipeds[parIndex++] = par;
                    par.transform(mm.originalMesh);
                    g.chunks.addVertexFall(par, mm);
                }
                else
                {
                    m.AutoTransformEnable = false;
                    mm.meshes[meshIndex++] = m;
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

            for (int i = cantBoxes; i < cantBoxes + putByHand; i++)
            {
                var par = mm.parallelipeds[i];
                par.transform(mm.originalMesh);
                g.chunks.addVertexFall(par, mm);
            }

            //partes de la iglesia no caen en ningun vertex, las agrego manualmente
            var addToChunk = new Action<TGCVector3>(v => {
                var c = g.chunks.fromCoordinates(v);
                if (!c.multimeshes.Contains(mm))
                {
                    c.multimeshes.Add(mm);
                }
            });

            addToChunk(new TGCVector3(-7168f, 0, 18671f));
            addToChunk(new TGCVector3(124, 0, -345));
            addToChunk(new TGCVector3(-1991, 0, -8930));
            addToChunk(new TGCVector3(-3159, 0, 3621));
            addToChunk(new TGCVector3(2258, 0, 3767));

            
            //centro de la iglesia ( -2879,753   -10917,6   3882,9  )

            foreach (var mesh in mm.meshes)
            {
                mesh.Effect = shader.comun;
                mesh.Technique = "DIFFUSE_MAP";
            }
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

            var texturesPath = g.game.MediaDir + "SkyBox\\";


            sky.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "lun4_up.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "lun4_dn.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lun4_lf2.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "lun4_rt2.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "lun4_bk2.jpg");
            sky.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "lun4_ft2.jpg");

            sky.SkyEpsilon = 25f;

            sky.Init();

        }

        public TgcMesh candleMesh;

        public bool isCandle(Meshc m) //prefiero hacer esto a tener un bool isCollectable en meshc
        {
            return m.mesh == candleMesh;
        }
        public void addCandles()
        {
            candleMesh = GetMeshFromScene("Vela-TgcScene.xml");
            Meshc.matrizChange = true;
            for (int i = 0; i < g.cameraSprites.candlesInMap; i++)
            {
                int j, k;
                do
                {
                    j = Random.Next(1, Chunks.chunksPerDim - 1);
                    k = Random.Next(1, Chunks.chunksPerDim - 1);

                    var pos = genPosInChunk(j, k);
                    pos.Y += 200f;

                    if (checkColission(pos,700f)) continue;

                    var candleMeshc = new Meshc();
                    //un poco excesivo que sea un meshc, lo eficiente seria que vela sea un tipo propio con una colision
                    //por radio. Pero hacer eso hace agregar un nuevo tipo de meshc, lo que podria hacer el codigo mas lento
                    //si sigo el camino de no usar polimorfismo. Igual ni ganas de agregar otro tipo. Que sea un meshc tiene
                    //los beneficios de que se va a poder deformar, y estas colisiones van a evitar que las velas se superpongan
                    //aunque agregar codigo para que se haga eso tampoco es una locura

                    candleMeshc.mesh = candleMesh;
                    candleMeshc.originalMesh = TGCMatrix.Scaling(new TGCVector3(10, 15, 10)) * TGCMatrix.Translation(pos);

                    var box = candleMeshc.mesh.BoundingBox;
                    var size = box.calculateSize();
                    var posb = box.calculateBoxCenter();

                    size.X *= 2.5f;
                    size.Y *= 10f;
                    size.Z *= 2.5f; //estaria bueno que tgc tenga multiplicacion miembro a miembro

                    candleMeshc.paralleliped = Parallelepiped.fromSizePosition(
                            size,
                            posb
                        );

                    //no uso  transformColission porque hace caer 4 vertices, lo que tiene potencial de registrar la 
                    //vela en mas de un chunk
                    candleMeshc.paralleliped.transform(GameModel.matriz * candleMeshc.originalMesh);
                    Chunk c = g.chunks.fromCoordinates(candleMeshc.paralleliped.transformedVertex[0]);
                    if (c != null)
                    {
                        c.meshes.Add(candleMeshc);
                    }

                    break;
                } while (true);
            }
            Meshc.matrizChange = false;
        }

        public bool checkColission(TGCVector3 pos,float colissionLen)
        {
            //no reutlizo las colisiones de camara porque estan muy ligadas a camara, y desligarlas lo haria mas lento
            //ademas pienso reescribir esa parte de camara asi que al pedo hacerla linda


            var chunk = g.chunks.fromCoordinates(pos);
            foreach (var mesh in chunk.meshes)
            {
                if (pointParallelipedXZColission(mesh.paralleliped, pos, colissionLen))
                    return true;
            }
            foreach (var multimesh in chunk.multimeshes)
            {
                foreach (var par in multimesh.parallelipeds)
                {
                    if (pointParallelipedXZColission(par, pos, colissionLen))
                        return true;
                }
            }
            return false;
        }

        public bool pointParallelipedXZColission(Parallelepiped par, TGCVector3 pos, float colissionLen)
        {
            var ray = new TgcRay();
            ray.Direction = new TGCVector3(1, 0, 0);
            ray.Origin = pos + new TGCVector3(-colissionLen, 0, 0);

            if (par.intersectRay(ray, out float t, out TGCVector3 q)&&t < 2f* colissionLen)
            {
                return true;
            }

            ray.Direction = new TGCVector3(0, 0, 1);
            ray.Origin = pos + new TGCVector3(0, 0, -colissionLen);

            if (par.intersectRay(ray, out t, out q)&&t < 2f* colissionLen)
            {
                return true;
            }
            return false;
        }

        TGCVector3 candlePlacePos = new TGCVector3(0f,-11220,0f);
        public int candlesPlaced=0;
        bool renderCandlePlace = false;
        public void updateCandlePlace()
        {
            //el candleplace podria implementarse como un tercer tipo de meshc tambien, pero como no estoy usando polimorfismo
            //agregar mas tipos haria el codigo mas lento,y aunque no lo fuera seria peor igual, y no se justifica solo por esto.
            //Pareceria que multimesh podria usarse, teniendo una sola colision e inicialmente ningun mesh, y se le van agregando
            //velas conforme se vayan poniendo. Pero no funcionaria porque multimesh esta hecho para manejar meshes unicas que
            //vienen de un conjunto, no puede manejar la misma mesh muchas veces con transformaciones distintas. 
            //Y aunque se pudiera se va a necesitar codigo especial para manejar el estado, y ya que estoy manejo todo ahi de
            //forma mas limpia y eficiente 

            if ((candlePlacePos - g.camera.eyePosition).LengthSq() < 9985474)
            {
                candlesPlaced = Math.Min(g.cameraSprites.candlesRequired, candlesPlaced + g.hands.state);
                g.hands.state = 0;
                g.mostro.mode = 0;
            }


            //si el centro de la iglesia se renderizó en el frame anterior, renderizar candleplace en este
            renderCandlePlace = g.chunks.chunks[41, 41].lastDrawnFrame == GameModel.actualFrame - 1 ||
                                g.chunks.chunks[40, 40].lastDrawnFrame == GameModel.actualFrame - 1;
            
        }

        public TGCVector3[] candlePlaceVertex;
        public void precomputeCandlePolygonVertex()
        {
            candlePlaceVertex = new TGCVector3[g.cameraSprites.candlesRequired];
            double radius = 1800d;
            double turnStep = 2d * 3.1415d / (double)g.cameraSprites.candlesRequired;
            for (int i = 0; i < g.cameraSprites.candlesRequired; i++)
            {
                var vertex = turnStep * i;
                candlePlaceVertex[i] = new TGCVector3(
                    candlePlacePos.X + (float)(Math.Cos(vertex) * radius), 
                    candlePlacePos.Y, 
                    candlePlacePos.Z + (float)(Math.Sin(vertex) * radius));
            }

        }

        public void renderCandles()
        {
            var scale=TGCMatrix.Scaling(new TGCVector3(10, 15, 10));

            for(int i = 0; i < candlesPlaced; i++)
            {
                candleMesh.Transform = scale * TGCMatrix.Translation(candlePlaceVertex[i]);
                candleMesh.Render();

                maybeLightCandleAt(candlePlaceVertex[i]);

                for (int j = 0; j < i; j++)
                {
                    TgcLine.fromExtremes(candlePlaceVertex[i], candlePlaceVertex[j], Color.Red).Render();
                }

            }
        }

        public void maybeLightCandleAt(TGCVector3 pos)
        {

            //si voy por el primer branch ilumino los mas cercanos entre los que estaban y los que se estan viendo
            //si voy por el segundo solo ilumino los que se estan viendo

            //por ahora me quedo con un punto intermedio, la primera mitad va a visibles y el resto a cercanos


            //me di cuenta que la segunda rama tiene un bug, puede insertar velas repetidas.
            //si agrego un chequeo para que no haga eso, y hay mas velas que su capacidad, va a saltar entre unas
            //y otras y causar flicker
            //la solucion a eso es limpiar el tramo ese cada frame, pero se lleva medio choto con la otra trama

            //la mejor solucion seria hacer un sistema distinto; mantener una lista ordenada de mas cercanos, presentes
            //y pasados. Los mas 4 mas lejanos pueden ser reemplazados entre frames


            //if (g.map.lightIndex < Map.lightCount/2)
            //if(g.map.lightIndex >= Map.lightCount)
            if(true)
            {
                //se priorizan las mas cercanas
                //creo que es mas rapido buscar el maximo cada vez que mantener la lista ordenada, no sé
                int maxIndex = 0;
                float maxDistSq = float.NegativeInfinity;
                for (int i = 0; i < Map.lightCount; i++)
                {
                    if (g.map.lightPosition[i] == pos)
                    //si la luz ya esta no volverla a poner
                    //una luz puede estar mas de una vez porque se reutilizan las de frames previos cuando
                    //en el triangulo actual hay menos de 9
                    {
                        return;
                    }
                    var dist = TGCVector3.LengthSq(g.map.lightPosition[i] - g.camera.eyePosition);
                    if (dist > maxDistSq)
                    {
                        maxDistSq = dist;
                        maxIndex = i;
                    }
                }
                if (TGCVector3.LengthSq(pos - g.camera.eyePosition) < maxDistSq)
                    g.map.lightPosition[maxIndex] = pos;
                lightIndex++;
            }
            else
            {
                lightPosition[lightIndex++] = pos;
            }
        }
    }
}