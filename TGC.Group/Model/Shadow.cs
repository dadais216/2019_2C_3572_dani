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

            var aspectRatio = D3DDevice.Instance.AspectRatio;
            var nearPlane = 50;
            var farPlane = 5000000;
            proj = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(80), aspectRatio, nearPlane, farPlane);


            var nearPlane2 = 50;
            var farPlane2 = 5000000;
            D3DDevice.Instance.Device.Transform.Projection = TGCMatrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f), aspectRatio, nearPlane2, farPlane2).ToMatrix();
        }


        public void render()
        {

            //esto es para el otro render
            //shader.SetValue("g_vLightPos", new TGCVector4(g_LightPos.X, g_LightPos.Y, g_LightPos.Z, 1));
            //shader.SetValue("g_vLightDir", new TGCVector4(g_LightDir.X, g_LightDir.Y, g_LightDir.Z, 1));

            var lightView = TGCMatrix.LookAtLH(g.mostro.pos, g.camera.eyePosition, new TGCVector3(0, 0, 1));

            shader.SetValue("g_mViewLightProj", (lightView * proj).ToMatrix());

            var screenRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            D3DDevice.Instance.Device.SetRenderTarget(0, tex.GetSurfaceLevel(0));
            var screenDS = D3DDevice.Instance.Device.DepthStencilSurface;


            D3DDevice.Instance.Device.DepthStencilSurface = depths;
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();

            g.terrain.renderForShadow(lightView * proj);
            g.chunks.renderForShadow();


            D3DDevice.Instance.Device.EndScene();
            //D3DDevice.Instance.Device.Present();


            //TextureLoader.Save("shadowmap.bmp", ImageFileFormat.Bmp, g_pShadowMap);

            D3DDevice.Instance.Device.DepthStencilSurface = screenDS;
            D3DDevice.Instance.Device.SetRenderTarget(0, screenRT);
        }


        public void renderMesh(Meshc mesh)
        {
            renderMesh(mesh.mesh, mesh.originalMesh, mesh.deformation,mesh.type);
        }
        public void renderMesh(MultiMeshc mmesh)
        {
            foreach(var mesh in mmesh.meshes)
            {
                renderMesh(mesh, mmesh.originalMesh, mmesh.deformation,mmesh.type);
            }
        }

        public void renderMesh(TgcMesh mesh,TGCMatrix originalMesh, TGCMatrix deformation, int type)
        {
            var effectPrev = mesh.Effect;
            var tecniquePrev = mesh.Technique;

            mesh.Effect = shader;
            mesh.Technique = "RenderShadow";

            shader.SetValue("type", type);

            mesh.Transform = Meshc.multMatrix(g.map.deforming, deformation) + originalMesh;
            mesh.Render();


            mesh.Effect = effectPrev;
            mesh.Technique = tecniquePrev;
        }

    }
}
