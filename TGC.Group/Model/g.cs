using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Input;
using TGC.Core.Terrain;

namespace TGC.Group.Model
{
    class g
    {
        //esta es una clase donde tiro todas las cosas para que sean globales
        //total todo es un singleton, no tiene sentido hacer scopes, ordenes de construccion y boludeces
        //las clases estaticas de c# son una cagada asi que hago esto
        //c# es una mentira
        static public Camera.Camera camera;
        static public GameModel game;
        static public Map map;
        static public Chunks chunks;
        static public Terrain terrain;
        static public Mostro mostro;
        static public Hands hands;
        static public CameraSprites cameraSprites;
        static public TgcD3dInput input;
        static public Shadow shadow;
    }
}
