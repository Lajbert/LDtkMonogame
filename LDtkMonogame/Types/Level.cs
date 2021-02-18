﻿using System;
using System.Collections.Generic;
using LDtk.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json.Linq;

namespace LDtk
{
    /// <summary>
    /// Abstracted version of ldtk's level
    /// </summary>
    public class Level
    {
        internal World owner;
        internal EntityInstance[] entities;
        internal IntGrid[] intGrids;

        /// <summary>
        /// The identifier of the level set in ldtk
        /// </summary>
        public string Identifier { get; internal set; }

        /// <summary>
        /// The Uid of the level set in ldtk
        /// </summary>
        public long Uid { get; internal set; }

        /// <summary>
        /// World position of the level
        /// </summary>
        public Vector2 Position { get; internal set; }

        /// <summary>
        /// World size in pixels of the level
        /// </summary>
        public Vector2 Size { get; internal set; }

        /// <summary>
        /// The clear color for the level
        /// </summary>
        public Color BgColor { get; internal set; }

        /// <summary>
        /// Prerendered layer textures created from <see cref="World.GetLevel(string)"/>
        /// </summary>
        public RenderTarget2D[] Layers { get; internal set; }

        /// <summary>
        /// The neighbours uids of the level
        /// </summary>
        public long[] Neighbours { get; internal set; }

        /// <summary>
        /// Gets the parsed entity from the ldtk json<br/>
        /// If fields are missing they will be logged to the console in debug mode only<br/>
        /// The class name must match the entities identifier id one is not specified<br/>
        /// </summary>
        /// <param name="identifier">The name of the entity to parse this to</param>
        /// <typeparam name="T">The class/struct you will use to parse the ldtk entity</typeparam>
        /// <returns>The entity cast to the class</returns>
        public T GetEntity<T>(string identifier = "") where T : new()
        {
            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                if (entities[entityIndex].Identifier == (identifier == "" ? typeof(T).Name : identifier))
                {
                    T entity = new T();

                    ParseBaseEntityFields<T>(entity, entities[entityIndex]);

                    for (int fieldIndex = 0; fieldIndex < entities[entityIndex].FieldInstances.Length; fieldIndex++)
                    {
                        ParseEntityFields<T>(entityIndex, entity, fieldIndex);
                    }

                    return entity;
                }
            }

            return new T();
        }

        /// <summary>
        /// Gets the parsed entities from the ldtk json<br/>
        /// If fields are missing they will be logged to the console in debug mode only<br/>
        /// The class name must match the entities identifier id one is not specified<br/>
        /// </summary>
        /// <param name="identifier">The name of the entity to parse this to</param>
        /// <typeparam name="T">The class/struct you will use to parse the ldtk entities</typeparam>
        /// <returns>The entities cast to the class</returns>
        public T[] GetEntities<T>(string identifier = "") where T : new()
        {
            List<T> ents = new List<T>();

            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                if (entities[entityIndex].Identifier == (identifier == "" ? typeof(T).Name : identifier))
                {
                    T entity = new T();

                    ParseBaseEntityFields<T>(entity, entities[entityIndex]);
                    for (int fieldIndex = 0; fieldIndex < entities[entityIndex].FieldInstances.Length; fieldIndex++)
                    {
                        ParseEntityFields<T>(entityIndex, entity, fieldIndex);
                    }

                    ents.Add(entity);
                }
            }

            return ents.ToArray();
        }

        private void ParseEntityFields<T>(int entityIndex, T entity, int fieldIndex) where T : new()
        {
            string variableName = entities[entityIndex].FieldInstances[fieldIndex].Identifier;

            variableName = char.ToLower(variableName[0]) + variableName.Substring(1);

            var field = typeof(T).GetField(variableName);

            if (field == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Field \"{variableName}\" not found add it to {typeof(T).FullName} for full support of LDtk entity");
                Console.ResetColor();
            }

            // Split any enums
            string[] variableTypes = entities[entityIndex].FieldInstances[fieldIndex].Type.Split('.');

            switch (variableTypes[0])
            {
                case "Int":
                case "Float":
                case "Bool":
                case "Enum":
                case "String":
                    field.SetValue(entity, Convert.ChangeType(entities[entityIndex].FieldInstances[fieldIndex].Value, field.FieldType));
                    break;

                case "LocalEnum":
                    field.SetValue(entity, Enum.Parse(field.FieldType, (string)entities[entityIndex].FieldInstances[fieldIndex].Value));
                    break;

                case "Color":
                    field.SetValue(entity, Utility.ConvertStringToColor(((string)entities[entityIndex].FieldInstances[fieldIndex].Value)[1..]));
                    break;

                case "Point":
                    JToken t = (JToken)entities[entityIndex].FieldInstances[fieldIndex].Value;
                    Vector2 point;
                    if (t != null)
                    {
                        point = new Vector2(t.First.First.Value<float>(), t.Last.Last.Value<float>());
                    }
                    else
                    {
                        point = new Vector2(0, 0);
                    }
                    field.SetValue(entity, point);
                    break;

                default:
                    throw new Exception("Unknown Variable of type " + entities[entityIndex].FieldInstances[fieldIndex].Type);
            }
        }

        private void ParseBaseEntityFields<T>(object entity, EntityInstance entityInstance)
        {
            // WorldPosition
            var worldPosition = typeof(T).GetField("position");
            if (worldPosition != null)
            {
                worldPosition.SetValue(entity, new Vector2(entityInstance.Px[0], entityInstance.Px[1]));
            }
#if DEBUG
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Sprite Field \"position\" not found add it to {typeof(T).FullName} for full support of LDtk entity or inherit LDtk.Entity class");
                Console.ResetColor();
            }
#endif

            // Pivot
            var pivot = typeof(T).GetField("pivot");
            if (pivot != null)
            {
                pivot.SetValue(entity, new Vector2((float)entityInstance.Pivot[0], (float)entityInstance.Pivot[1]));
            }
#if DEBUG
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Sprite Field \"pivot\" not found add it to {typeof(T).FullName} for full support of LDtk entity or inherit LDtk.Entity class");
                Console.ResetColor();
            }
#endif

            // Texture
            var texture = typeof(T).GetField("texture");
            if (texture != null)
            {
                Texture2D tileset = owner.GetTilesetTextureFromUid(entityInstance.Tile.TilesetUid);
                texture.SetValue(entity, tileset);
            }
#if DEBUG
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Sprite Field \"texture\" not found add it to {typeof(T).FullName} if you need texture support on this entity or inherit LDtk.Entity class");
                Console.ResetColor();
            }
#endif

            // FrameSize
            var frameSize = typeof(T).GetField("size");
            if (frameSize != null)
            {
                var entityDefinition = owner.GetEntityDefinitionFromUid(entityInstance.DefUid);
                frameSize.SetValue(entity, new Vector2(entityDefinition.Width, entityDefinition.Height));
            }
#if DEBUG
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Sprite Field \"size\" not found add it to {typeof(T).FullName} if you need texture support on this entity or inherit LDtk.Entity class");
                Console.ResetColor();
            }
#endif
        }

        /// <summary>
        /// Gets an <see cref="IntGrid"/> from an identifier
        /// </summary>
        /// <param name="identifier">Identifier of an intgrid</param>
        public IntGrid GetIntGrid(string identifier)
        {
            for (int i = 0; i < intGrids.Length; i++)
            {
                if (intGrids[i].identifier == identifier)
                {
                    return intGrids[i];
                }
            }

            throw new Exception(identifier + " IntGrid not found!");
        }
    }
}