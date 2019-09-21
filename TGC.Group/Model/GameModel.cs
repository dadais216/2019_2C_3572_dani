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

        Map map;

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
            map = new Map(this);
        }




        

        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aquí todo el código de inicialización: cargar modelos, texturas, estructuras de optimización, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {

            map.Init();

            matriz = map.collisions[0].Transform;

            // Instancio camara
            Camara = new Camera.Camera(Input,map);


        }

        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la lógica de computo del modelo, así como también verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        /// 

        TGCMatrix matriz;
        public override void Update()
        {
            PreUpdate();


            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.Y)) matriz.M11 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.U)) matriz.M12 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.I)) matriz.M13 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.H)) matriz.M21 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.J)) matriz.M22 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.K)) matriz.M23 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.B)) matriz.M31 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.N)) matriz.M32 += ElapsedTime;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.M)) matriz.M33 += ElapsedTime;

            //ni idea de por que la traslacion es tan lenta
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D1)) matriz.M41 += ElapsedTime * 5000;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D2)) matriz.M42 += ElapsedTime * 5000;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D3)) matriz.M43 += ElapsedTime * 5000;

            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D4)) matriz.M14 += 0.1f;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D5)) matriz.M24 += 0.1f;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D6)) matriz.M34 += 0.1f;
            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.D7)) matriz.M44 += 0.1f;


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
            map.Render(matriz);
            PostRender();
        }

        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            map.scene.Clear();
        }

        
    }
}