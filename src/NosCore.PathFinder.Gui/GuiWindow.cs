﻿//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Shared.Enumerations.Map;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace NosCore.PathFinder.Gui
{
    public class GuiWindow : GameWindow
    {
        private readonly byte _gridsize;
        private readonly Map _map;
        private readonly List<MapMonster> _monsters;
        private readonly List<MapNpc> _npcs;
        private readonly int _originalHeight;
        private readonly int _originalWidth;
        private readonly List<Tuple<short, short, byte>> _walls = new List<Tuple<short, short, byte>>();
        private double _gridsizeX;
        private double _gridsizeY;

        public GuiWindow(Map map, byte gridsize, int width, int height, GraphicsMode mode, string title) : base(
            width * gridsize, height * gridsize, mode, title)
        {
            _originalWidth = width * gridsize;
            _originalHeight = height * gridsize;
            _map = map;
            _gridsizeX = gridsize;
            _gridsizeY = gridsize;
            _gridsize = gridsize;
            _monsters = DaoFactory.MapMonsterDao.Where(s => s.MapId == map.MapId).Adapt<List<MapMonster>>();
            var npcMonsters = DaoFactory.NpcMonsterDao.LoadAll().ToList();
            var mapInstance =
                new MapInstance(map, new Guid(), false, MapInstanceType.BaseMapInstance, npcMonsters)
                {
                    IsSleeping = false
                };
            foreach (var mapMonster in _monsters)
            {
                mapMonster.PositionX = mapMonster.MapX;
                mapMonster.PositionY = mapMonster.MapY;
                mapMonster.MapInstance = mapInstance;
                mapMonster.MapInstanceId = mapInstance.MapInstanceId;
                mapMonster.Mp = 100;
                mapMonster.Hp = 100;
                mapMonster.Speed = npcMonsters.Find(s => s.NpcMonsterVNum == mapMonster.MapId)?.Speed ?? 0;
                mapMonster.IsAlive = true;
            }

            _npcs = DaoFactory.MapNpcDao.Where(s => s.MapId == map.MapId).Cast<MapNpc>().ToList();
            foreach (var mapNpc in _npcs)
            {
                mapNpc.PositionX = mapNpc.MapX;
                mapNpc.PositionY = mapNpc.MapY;
                mapNpc.MapInstance = mapInstance;
                mapNpc.MapInstanceId = mapInstance.MapInstanceId;
                mapNpc.Mp = 100;
                mapNpc.Hp = 100;
                mapNpc.Speed = npcMonsters.Find(s => s.NpcMonsterVNum == mapNpc.MapId)?.Speed ?? 0;
                mapNpc.IsAlive = true;
            }

            Parallel.ForEach(_monsters.Where(s => s.Life == null), monster => monster.StartLife());
            Parallel.ForEach(_npcs.Where(s => s.Life == null), npc => npc.StartLife());
            GetMap();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
            {
                Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.ClearColor(Color.LightSkyBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _gridsizeX = _gridsize * (ClientRectangle.Width / (double) _originalWidth);
            _gridsizeY = _gridsize * (ClientRectangle.Height / (double) _originalHeight);
            var world = Matrix4.CreateOrthographicOffCenter(0, ClientRectangle.Width, ClientRectangle.Height, 0, 0, 1);
            GL.LoadMatrix(ref world);
            foreach (var wall in _walls)
            {
                DrawPixel(wall.Item1, wall.Item2, Color.Blue); //TODO iswalkable
            }

            foreach (var monster in _monsters)
            {
                DrawPixel(monster.PositionX, monster.PositionY, Color.Red);
            }

            foreach (var npc in _npcs)
            {
                DrawPixel(npc.PositionX, npc.PositionY, Color.Yellow);
            }

            GL.Flush();
            SwapBuffers();
            Thread.Sleep(32);
        }

        private void GetMap()
        {
            for (short i = 0; i < _map.YLength; i++)
            {
                for (short t = 0; t < _map.XLength; t++)
                {
                    var value = _map[t, i];
                    if (_map[t, i] > 0)
                    {
                        _walls.Add(new Tuple<short, short, byte>(t, i, value));
                    }
                }
            }
        }

        private void DrawPixel(short x, short y, Color color)
        {
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(color);
            GL.Vertex2(x * _gridsizeX, y * _gridsizeY);
            GL.Vertex2(_gridsizeX * (x + 1), y * _gridsizeY);
            GL.Vertex2(_gridsizeX * (x + 1), _gridsizeY * (y + 1));
            GL.Vertex2(x * _gridsizeX, _gridsizeY * (y + 1));
            GL.End();
        }
    }
}