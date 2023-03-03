#define USE_MATERIALS
#define USE_MESHES

using System;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;

namespace Psych
{
    namespace NativeMath
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

    namespace NativeMaterialInfo
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
    public struct ManagedMaterial
    {
        public struct ManagedPropertyData
        {
            public NativeMaterialInfo.PropertyType nativePropertyType;
            public IntPtr nativeTextureRelativeFileName;
            public string ManagedTextureRelativeFileName => Marshal.PtrToStringAnsi(nativeTextureRelativeFileName);

            public IntPtr nativeTextureAbsoluteFilePath;
            public string ManagedTextureAbsoluteFilePath => Marshal.PtrToStringAnsi(nativeTextureAbsoluteFilePath);

            public NativeMath.Vector4 nativeDataColorValues;
        };

        public NativeMaterialInfo.MaterialType nativeMaterialType;
        public IntPtr nativeMaterialProperties;

        public ManagedPropertyData[] ManagedMaterialProperties =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<ManagedPropertyData>(nativeMaterialProperties, (int)NativeMaterialInfo.PropertyType.PROPERTYTYPE_COUNT);

        public Texture2D[] ManagedTextures
        {
            get
            {
                Texture2D[] result = new Texture2D[nativeTextureCount];

                int currentTextureIndex = 0;
                var props = ManagedMaterialProperties;
                for (int i = 0; i < props.Length; i++)
                {
                    if (props[i].ManagedTextureRelativeFileName != null ||
                        props[i].ManagedTextureAbsoluteFilePath != null)
                    {
                        result[currentTextureIndex] = MarshalHelpers.LoadPNG(props[i].ManagedTextureAbsoluteFilePath);
                        ++currentTextureIndex;
                    }
                }
        
                return result;
            }
        }

        public uint nativeTextureCount;
    }
    #endif

    #if USE_MESHES
    public struct ManagedMesh
    {
        public IntPtr nativeAllVerticesPositions;
        public NativeMath.Vector3[] ManagedVertexPositions =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<NativeMath.Vector3>(nativeAllVerticesPositions, (int)nativeVertexCount);

        public IntPtr nativeNormals;

        public NativeMath.Vector3[] ManagedNormals =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<NativeMath.Vector3>(nativeNormals, (int)nativeVertexCount);

        public IntPtr nativeUvs;

        public NativeMath.Vector2[] ManagedUVs =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<NativeMath.Vector2>(nativeUvs, (int)nativeVertexCount);

        public IntPtr nativeIndices;

        public uint[] ManagedIndices =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<uint>(nativeIndices, (int)nativeIndexCount);

        public int[] ConvertedIndices =>
            Array.ConvertAll(ManagedIndices, val => checked((int)val));

        public uint nativeVertexCount;
        public uint nativeIndexCount;
    }
    #endif

    public struct ManagedObject
    {
        public int nativeParentArrayIndexID;
        public IntPtr nativeChildrenArrayIndexIDs;

    #if USE_MESHES
        public IntPtr nativeMesh;

        public ManagedMesh ManagedMesh => Marshal.PtrToStructure<ManagedMesh>(nativeMesh);
    #endif
    #if USE_MATERIALS
        public IntPtr nativeMaterials;

        public ManagedMaterial[] ManagedMaterials => 
            MarshalHelpers.MarshalUnmanagedArrayToStruct<ManagedMaterial>(nativeMaterials, (int)nativeNumberOfMaterials);
    #endif

        public uint nativeNumberOfChildren;
        public uint nativeNumberOfMaterials;

        public IntPtr nativeName;

        public string ManagedName =>
            Marshal.PtrToStringAnsi(nativeName);

        public uint nativeArrayIndexID;
    }

    public struct ManagedImportedFbxScene
    {
        public IntPtr nativeObjects;
        public uint numberOfNativeObjects;

        public ManagedObject[] ManagedObjects =>
            MarshalHelpers.MarshalUnmanagedArrayToStruct<ManagedObject>(nativeObjects, (int) numberOfNativeObjects);
    }
    
    public struct ManagedFbxHandler
    {
        public IntPtr nativeFbxScene;

        public ManagedImportedFbxScene ManagedFBXScene => Marshal.PtrToStructure<ManagedImportedFbxScene>(nativeFbxScene);
    }

    public static class MarshalHelpers
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
            T[] managedArray = new T[length];

            for (int i = 0; i < length; i++)
            {
                IntPtr ins = new IntPtr(unmanagedArray.ToInt64() + i * size);
                managedArray[i] = Marshal.PtrToStructure<T>(ins);
            }

            return managedArray;
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
    #endregion
}
