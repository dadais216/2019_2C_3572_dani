using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Direct3D;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;

namespace TGC.Group.Model
{
    class Shadow
    {
        public Texture tex;
        Surface depths;
        TGCMatrix proj;
        int SHADOWMAP_SIZE = 1024;
        public Effect shader;

        public Shadow()
        {
            g.shadow = this;

            shader=TGCShaders.Instance.LoadEffect(TGCShaders.Instance.CommonShadersPath + "shadow.fx");

            tex = new Texture(D3DDevice.Instance.Device, SHADOWMAP_SIZE, SHADOWMAP_SIZE, 1, Usage.RenderTarget, Format.R32F, Pool.Default);
            depths = D3DDevice.Instance.Device.CreateDepthStencilSurface(SHADOWMAP_SIZE, SHADOWMAP_SIZE, DepthFormat.D24S8, MultiSampleType.None, 0, true);

            g.map.shader.SetValue("shadowTexture", tex);

            var aspectRatio = D3DDevice.Instance.AspectRatio;
            var nearPlane = 50;
            var farPlane = 5000000;
            proj = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(80), aspectRatio, nearPlane, farPlane);


            //var nearPlane2 = 50;
            //var farPlane2 = 5000000;
            //D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f), aspectRatio, nearPlane2, farPlane2).ToMatrix();
            //ni idea de que hace esta linea pero me hizo perder una hora cuando cambie el orden de inicializacion
            //y cambio la matriz esta, que solo afectaba a las velas en mano y la barra de estamina
        }


        public void render()
        {
            //pense en poner la luz adentro de la calabera y hacer que se vean los bordes,
            //se veia medio feo pero por ahi se puede hacer mejor
            //var lightPos =  g.mostro.cPos 
            //                + TGCVector3.Up * 400f 
            //                + g.mostro.colDir * 0.21f 
            //                + TGCVector3.Cross(g.mostro.colDir, TGCVector3.Up) * .1f;

            var lightPos = g.mostro.cPos + g.mostro.height * TGCVector3.Up;

            var lightObj = new TGCVector3(g.mostro.lightObj.X, g.mostro.flyHeight, g.mostro.lightObj.Y);

            var lightView = TGCMatrix.LookAtLH(lightPos, lightObj, new TGCVector3(0, 0, 1));
            var viewLightProj = (lightView * proj).ToMatrix();
            shader.SetValue("mViewLightProj", viewLightProj);
            g.map.shader.SetValue("mViewLightProj", viewLightProj);

            g.map.shader.SetValue("lightPos", TGCVector3.Vector3ToFloat3Array(lightPos));

            var lightDir = lightObj - lightPos;
            lightDir.Normalize();
            g.map.shader.SetValue("lightDir", TGCVector3.Vector3ToFloat3Array(lightDir));


            var screenRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            D3DDevice.Instance.Device.SetRenderTarget(0, tex.GetSurfaceLevel(0));
            var screenDS = D3DDevice.Instance.Device.DepthStencilSurface;


            D3DDevice.Instance.Device.DepthStencilSurface = depths;
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.CornflowerBlue, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();


            //g.mostro.renderForShadow();
            g.terrain.renderForShadow(viewLightProj);
            g.chunks.renderForShadow();

            //uso el mesh del esqueleto para el jugador porque no tengo otro
            //podria ser deep lore
            var mostroPos = g.mostro.pos;
            g.mostro.pos = g.camera.eyePosition- 500f*TGCVector3.Up;
            g.mostro.render();
            g.mostro.pos = mostroPos;

            D3DDevice.Instance.Device.EndScene();
            //D3DDevice.Instance.Device.Present();


            //TextureLoader.Save("shadowmap.bmp", ImageFileFormat.Bmp, g_pShadowMap);

            D3DDevice.Instance.Device.DepthStencilSurface = screenDS;
            D3DDevice.Instance.Device.SetRenderTarget(0, screenRT);

            g.map.shader.SetValue("inView", g.mostro.timeInView > 0);
        }

    }
}
