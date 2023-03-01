#define USE_MATERIALS
#define USE_MESHES

using System;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;

namespace Psych
{
    namespace CMath
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Vector2
        {
            public float x;
            public float y;

            public override string ToString()
            {
                return $"{x}, {y}";
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Vector3
        {
            public float x;
            public float y;
            public float z;

            public override string ToString()
            {
                return $"{x}, {y}, {z}";
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Vector4
        {
            public float x;
            public float y;
            public float z;
            public float w;

            public override string ToString()
            {
                return $"{x}, {y}, {z}, {w}";
            }
        }
    }

    namespace CMaterialInfo
    {
        public enum PropertyType
        {
            PROPERTYTYPE_EMISSIVE = 0,
            PROPERTYTYPE_AMBIENT,
            PROPERTYTYPE_DIFFUSE,
            PROPERTYTYPE_NORMAL,
            PROPERTYTYPE_BUMP,
            PROPERTYTYPE_TRANSPARENCY,
            PROPERTYTYPE_DISPLACEMENT,
            PROPERTYTYPE_VECTOR_DISPLACEMENT,
            PROPERTYTYPE_SPECULAR,
            PROPERTYTYPE_SHININESS,
            PROPERTYTYPE_REFLECTION,
            PROPERTYTYPE_COUNT
        };

        public enum MaterialType
        {
            MATERIALTYPE_PHONG = 0,
            MATERIALTYPE_LAMBERT
        };
    }

    enum CRESULT
    {
        CRESULT_SUCCESS = 0,
        CRESULT_INCORRECT_FILE_PATH,
        CRESULT_NO_OBJECTS_IN_SCENE,
        CRESULT_NODE_WAS_NOT_GEOMETRY_TYPE,
        CRESULT_ROOT_NODE_NOT_FOUND
    };

    #region Class Definitions

    #if USE_MATERIALS
    public struct CPPMaterial
    {
        public struct CPPPropertyData
        {
            public CMaterialInfo.PropertyType m_propertyType;
            public IntPtr m_textureRelativeFileName;
            public string TextureRelativeFileName => Marshal.PtrToStringAnsi(m_textureRelativeFileName);

            public IntPtr m_textureAbsoluteFilePath;
            public string TextureAbsoluteFilePath => Marshal.PtrToStringAnsi(m_textureAbsoluteFilePath);

            public CMath.Vector4 m_dataColorValues;
        };

        public CMaterialInfo.MaterialType m_materialType;
        public IntPtr m_materialProperties;

        public CPPPropertyData[] MaterialProperties =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<CPPPropertyData>(m_materialProperties, (int)CMaterialInfo.PropertyType.PROPERTYTYPE_COUNT);

        public Texture2D[] Textures
        {
            get
            {
                Texture2D[] result = new Texture2D[m_textureCount];

                int currentTextureIndex = 0;
                var props = MaterialProperties;
                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i].TextureRelativeFileName != null ||
                        props[i].TextureAbsoluteFilePath != null)
                    {
                        result[currentTextureIndex] = MarshalHelpers.LoadPNG(props[i].TextureAbsoluteFilePath);
                        ++currentTextureIndex;
                    }
                }
        
                return result;
            }
        }

        public uint m_textureCount;
    }
    #endif

    #if USE_MESHES
    public struct CPPMesh
    {
        public IntPtr m_allVerticesPositions;
        public CMath.Vector3[] VertexPositions =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<CMath.Vector3>(m_allVerticesPositions, (int)m_vertexCount);

        public IntPtr m_normals;

        public CMath.Vector3[] Normals =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<CMath.Vector3>(m_normals, (int)m_vertexCount);

        public IntPtr m_uvs;

        public CMath.Vector2[] UVs =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<CMath.Vector2>(m_uvs, (int)m_vertexCount);

        public IntPtr m_indices;

        public uint[] Indices =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<uint>(m_indices, (int)m_indexCount);

        public int[] ConvertedIndices =>
            Array.ConvertAll(Indices, val => checked((int)val));

        public uint m_vertexCount;
        public uint m_indexCount;
    }
    #endif

    public struct CPPObject
    {
        public int m_parentArrayIndexID;
        public IntPtr m_childrenArrayIndexIDs;

    #if USE_MESHES
        public IntPtr m_mesh;

        public CPPMesh CPPMesh => Marshal.PtrToStructure<CPPMesh>(m_mesh);
    #endif
    #if USE_MATERIALS
        public IntPtr m_materials;

        public CPPMaterial[] CPPMaterials => 
            MarshalHelpers.MarshalUnmanagedArrayToStruct<CPPMaterial>(m_materials, (int)m_numberOfMaterials);
    #endif

        public uint m_numberOfChildren;
        public uint m_numberOfMaterials;

        public IntPtr m_name;

        public string Name =>
            Marshal.PtrToStringAnsi(m_name);

        public uint m_arrayIndexID;
    }

    public struct CPPImportedFBXScene
    {
        public IntPtr m_objects;
        public uint m_numberOfObjects;

        public CPPObject[] CPPObjects =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<CPPObject>(m_objects, (int) m_numberOfObjects);
    }

    public class MarshalHelpers
    {
        public static void MarshalUnmanagedArrayToStruct<T>(IntPtr unmanagedArray, int length, out T[] managedArray)
        {
            var size = Marshal.SizeOf(typeof(T));
            managedArray = new T[length];

            for (int i = 0; i < length; i++)
            {
                IntPtr ins = new IntPtr(unmanagedArray.ToInt64() + i * size);
                managedArray[i] = Marshal.PtrToStructure<T>(ins);
            }
        }

        public static T[] MarshalUnmanagedArrayToStruct<T>(IntPtr unmanagedArray, int length)
        {
            var size = Marshal.SizeOf(typeof(T));
            T[] mangagedArray = new T[length];

            for (int i = 0; i < length; i++)
            {
                IntPtr ins = new IntPtr(unmanagedArray.ToInt64() + i * size);
                mangagedArray[i] = Marshal.PtrToStructure<T>(ins);
            }

            return mangagedArray;
        }
        
        public static Texture2D LoadPNG(string _filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(_filePath))
            {
                fileData = File.ReadAllBytes(_filePath);
                tex = new Texture2D(1, 1);
                tex.LoadImage(fileData); // LoadImage() auto resizes the texture dimensions.
            }

            else
            {
                return null;
            }

            return tex;
        }
    }

    public struct CPPFBXHandler
    {
        public IntPtr m_fbxScene;

        public CPPImportedFBXScene CPPFBXScene => Marshal.PtrToStructure<CPPImportedFBXScene>(m_fbxScene);
    }
    #endregion
}
