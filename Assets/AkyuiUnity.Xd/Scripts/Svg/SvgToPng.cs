using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.VectorGraphics;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace AkyuiUnity.Xd
{
    public static class SvgToPng
    {
        public static VectorUtils.TessellationOptions TessellationOptions => new VectorUtils.TessellationOptions
        {
            StepDistance = 100.0f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f
        };

        public static byte[] Convert(string svg, Vector2Int size)
        {
            using (var reader = new StringReader(svg))
            {
                var sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions.DontPreserve);
                var tessOptions = TessellationOptions;
                var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
                var sprite = BuildSprite(geometry, Rect.zero, 1f, VectorUtils.Alignment.BottomLeft, Vector2.zero, 64);

                var svgImporter = new SVGImporter();
                var method = typeof(SVGImporter).GetMethod("MaterialForSVGSprite", BindingFlags.Instance | BindingFlags.NonPublic);
                var mat = (Material) method.Invoke(svgImporter, new object[] { sprite });

                var tex = VectorUtils.RenderSpriteToTexture2D(sprite, size.x, size.y, mat);
                return tex.EncodeToPNG();
            }
        }

        // VectorUtils.BuildSprite
        private static Sprite BuildSprite(List<VectorUtils.Geometry> geoms, Rect rect, float svgPixelsPerUnit, VectorUtils.Alignment alignment,
            Vector2 customPivot, UInt16 gradientResolution, bool flipYAxis = false)
        {
            // Generate atlas
            var texAtlas = VectorUtils.GenerateAtlasAndFillUVs(geoms, gradientResolution);

            List<Vector2> vertices;
            List<UInt16> indices;
            List<Color> colors;
            List<Vector2> uvs;
            List<Vector2> settingIndices;
            // FillVertexChannels(geoms, 1.0f, texAtlas != null, out vertices, out indices, out colors, out uvs, out settingIndices, flipYAxis);
            var parameters = new object[]
            {
                geoms,
                1.0f,
                texAtlas != null,
                null,
                null,
                null,
                null,
                null,
                flipYAxis
            };
            CallVectorUtils("FillVertexChannels", parameters);
            vertices = (List<Vector2>) parameters[3];
            indices = (List<UInt16>) parameters[4];
            colors = (List<Color>) parameters[5];
            uvs = (List<Vector2>) parameters[6];
            settingIndices = (List<Vector2>) parameters[7];

            Texture2D texture = texAtlas != null ? texAtlas.Texture : null;

            if (rect == Rect.zero)
            {
                rect = VectorUtils.Bounds(vertices);
                VectorUtils.RealignVerticesInBounds(vertices, rect, flipYAxis);
            }
            else if (flipYAxis)
            {
                VectorUtils.FlipVerticesInBounds(vertices, rect);

                // The provided rect should normally contain the whole geometry, but since VectorUtils.SceneNodeBounds doesn't
                // take the strokes into account, some triangles may appear outside the rect. We clamp the vertices as a workaround for now.
                // VectorUtils.ClampVerticesInBounds(vertices, rect);
                CallVectorUtils("ClampVerticesInBounds",
                    new object[]
                    {
                        vertices,
                        rect,
                    });
            }

            // var pivot = VectorUtils.GetPivot(alignment, customPivot, rect, flipYAxis);
            var pivot = CallVectorUtils<Vector2>("GetPivot",
                new object[]
                {
                    alignment,
                    customPivot,
                    rect,
                    flipYAxis,
                });

            // The Sprite.Create(Rect, Vector2, float, Texture2D) method is internal. Using reflection
            // until it becomes public.
            var spriteCreateMethod = typeof(Sprite).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[]
            {
                typeof(Rect), typeof(Vector2), typeof(float), typeof(Texture2D)
            }, null);
            var sprite = spriteCreateMethod.Invoke(null, new object[] { rect, pivot, svgPixelsPerUnit, texture }) as Sprite;

            // sprite.OverrideGeometry(vertices.ToArray(), indices.ToArray());
            using (var texCoordNativeArray = new NativeArray<Vector2>(vertices.Select(x => Vector2.zero).ToArray(), Allocator.Temp))
            using (var verticesNativeArray = new NativeArray<Vector3>(vertices.Select(x => (Vector3) x).ToArray(), Allocator.Temp))
            using (var indicesNativeArray = new NativeArray<ushort>(indices.ToArray(), Allocator.Temp))
            {
                sprite.SetIndices(indicesNativeArray);
                sprite.SetVertexCount(verticesNativeArray.Length);
                sprite.SetVertexAttribute(VertexAttribute.Position, verticesNativeArray);
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, texCoordNativeArray);
            }

            if (colors != null)
            {
                var colors32 = colors.Select(c => (Color32) c);
                using (var nativeColors = new NativeArray<Color32>(colors32.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Color32>(VertexAttribute.Color, nativeColors);
            }

            if (uvs != null)
            {
                using (var nativeUVs = new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord0, nativeUVs);
                using (var nativeSettingIndices = new NativeArray<Vector2>(settingIndices.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord2, nativeSettingIndices);
            }

            return sprite;
        }

        private static T CallVectorUtils<T>(string methodName, object[] parameters)
        {
            var method = typeof(VectorUtils).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return (T) method.Invoke(null, parameters);
        }

        private static void CallVectorUtils(string methodName, object[] parameters)
        {
            var method = typeof(VectorUtils).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, parameters);
        }
    }
}