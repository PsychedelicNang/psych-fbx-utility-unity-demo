using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Psych
{
    public static class PsychFbxUtilityDll
    {
        [DllImport("psychfbxutility")]
        public static extern IntPtr CreateFBXHandler();

        [DllImport("psychfbxutility")]
        public static extern void DestroyFBXHandler(IntPtr fbxHandlerObject);

        [DllImport("psychfbxutility", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadFBXFile(IntPtr fbxHandlerObject, string fbxFilePath);
    }
    
    public class PsychFbxUtility : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int BumpScale = Shader.PropertyToID("_BumpScale");
        private static readonly int Metallic = Shader.PropertyToID("Metallic");
        private static readonly int MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
        private static readonly int Smoothness = Shader.PropertyToID("Smoothness");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        
        private ManagedFbxHandler managedFbxHandler;
        private IntPtr unmanagedFbxHandler;

        private MeshFilter         unityMeshFilter;
        private Mesh               unityMesh;
        private MeshRenderer       unityMeshRenderer;
        private Transform          unityObjectTransform;
       
        public List<GameObject>    unityGameObjects;

        #region Class Methods
        
        void Start () 
        {
            unityMeshFilter = gameObject.AddComponent<MeshFilter>();
            unityMesh = unityMeshFilter.mesh;
            unityMeshRenderer = gameObject.AddComponent<MeshRenderer>();
            unityObjectTransform = gameObject.GetComponent<Transform>();

            // C++ handler which is unmanaged memory (we need to delete by calling DLLDestroyFBXHandler())
            unmanagedFbxHandler = PsychFbxUtilityDll.CreateFBXHandler();

            DateTime timeBeforeFileLoad = DateTime.Now;

            var fbxPath = "../Submodules/psych-fbx-utility/psych-fbx-utility-vs/resources/SM_PistolArnold.fbx";

            CRESULT result = (CRESULT)PsychFbxUtilityDll.LoadFBXFile(unmanagedFbxHandler, fbxPath);
		    DateTime timeAfterFileLoad = DateTime.Now;
		    print (result);
            var durationFileLoad = timeAfterFileLoad - timeBeforeFileLoad;

            switch (result)
            {
                case CRESULT.CRESULT_SUCCESS:
                    {
                        DateTime timeBeforeFileParsed = DateTime.Now;
                        ParseFbxHandler();
                        DateTime timeAfterFileParsed = DateTime.Now;

                        var durationFileParsed = timeAfterFileParsed - timeBeforeFileParsed;

                        if (durationFileLoad.Seconds > 0)
                            print("The FBX File loaded in: " + durationFileLoad.Seconds + "." + durationFileLoad.Milliseconds + " s");
                        else
                            print("The FBX File loaded in: " + durationFileLoad.Milliseconds + " ms");

                        if (durationFileParsed.Seconds > 0)
                            print("The FBX File was parsed in: " + durationFileParsed.Seconds + "." + durationFileParsed.Milliseconds + " s");
                        else
                            print("The FBX File was parsed in: " + durationFileParsed.Milliseconds + " ms");
                    }
                    break;
                case CRESULT.CRESULT_INCORRECT_FILE_PATH:
                    print("Incorrect File Path.");
                    break;
                case CRESULT.CRESULT_NO_OBJECTS_IN_SCENE:
                    print("There were no objects in the given FBX scene.");
                    break;
                case CRESULT.CRESULT_NODE_WAS_NOT_GEOMETRY_TYPE:
                    print("Please, make sure the FBX scene only contains geometry nodes.");
                    break;
                case CRESULT.CRESULT_ROOT_NODE_NOT_FOUND:
                    print("The root node of the FBX file was not found.");
                    break;
                default:
                    break;
            }

            unityObjectTransform.localScale = new Vector3(10.0f, 10.0f, 10.0f);
            unityObjectTransform.Translate(new Vector3(0.0f, 3.0f, 0.0f));
        }

        private void ParseFbxHandler()
        {
            managedFbxHandler = (ManagedFbxHandler)Marshal.PtrToStructure(unmanagedFbxHandler, typeof(ManagedFbxHandler));

            unityGameObjects = new List<GameObject>((int)managedFbxHandler.ManagedFBXScene.numberOfNativeObjects);

            ManagedObject[] sceneObjects = managedFbxHandler.ManagedFBXScene.ManagedObjects;

            for (uint currObjectIndex = 0;
                 currObjectIndex < (int)managedFbxHandler.ManagedFBXScene.numberOfNativeObjects;
                 currObjectIndex++)
            {
                GameObject unityGameObject = new GameObject();
                unityGameObjects.Add(unityGameObject);

                MeshFilter currMeshFilter = unityGameObject.AddComponent<MeshFilter>();
                MeshRenderer currMeshRenderer = unityGameObject.AddComponent<MeshRenderer>();

                var currentManagedObject = sceneObjects[currObjectIndex];
                unityGameObject.name = currentManagedObject.ManagedName;

                print("Object: " + currentManagedObject.ManagedName + "\tMaterial Count: " + currentManagedObject.nativeNumberOfMaterials + "\tNumber of Children: " + currentManagedObject.nativeNumberOfChildren);

                // Mesh info
                FillOutMeshFilter(currMeshFilter, currentManagedObject);
                
                // Material info
                FillOutMeshRenderer(currMeshRenderer, currentManagedObject);
            }
            
            for (uint currObjectIndex = 0; currObjectIndex < managedFbxHandler.ManagedFBXScene.numberOfNativeObjects; currObjectIndex++)
            {
                unityGameObjects[(int)currObjectIndex].transform.parent = unityGameObjects[(int)sceneObjects[currObjectIndex].nativeParentArrayIndexID].transform;
            }

            unityGameObjects[0].transform.parent = unityObjectTransform;
        }
        
        
        private bool FillOutMeshFilter(MeshFilter targetMeshFilter, ManagedObject managedObject)
        {
            var managedMesh = managedObject.ManagedMesh;
            if (managedMesh.nativeVertexCount > 0)
            {
                NativeMath.Vector3[] vertices = managedMesh.ManagedVertexPositions;
                NativeMath.Vector3[] normals = managedMesh.ManagedNormals;
                NativeMath.Vector2[] uvs = managedMesh.ManagedUVs;

                Vector3[] unityVerts = new Vector3[managedMesh.nativeVertexCount];
                Vector3[] unityNormals = new Vector3[managedMesh.nativeVertexCount];
                Vector2[] unityUVs = new Vector2[managedMesh.nativeVertexCount];
                
                for (uint currVertex = 0; currVertex < managedMesh.nativeVertexCount; currVertex++)
                {
                    unityVerts[currVertex].x = vertices[currVertex].x;
                    unityVerts[currVertex].y = vertices[currVertex].y;
                    unityVerts[currVertex].z = vertices[currVertex].z;

                    unityNormals[currVertex].x = normals[currVertex].x;
                    unityNormals[currVertex].y = normals[currVertex].y;
                    unityNormals[currVertex].z = normals[currVertex].z;

                    unityUVs[currVertex].x = uvs[currVertex].x;
                    unityUVs[currVertex].y = uvs[currVertex].y;
                }

                var mesh = targetMeshFilter.mesh;
                mesh.vertices = unityVerts;
                mesh.normals = unityNormals;
                mesh.uv = unityUVs;
                mesh.triangles = managedMesh.ConvertedIndices;
                
                return true;
            }

            return false;
        }

        private bool FillOutMeshRenderer(MeshRenderer targetMeshRenderer, ManagedObject managedObject)
        {
            if (managedObject.nativeNumberOfMaterials > 0)
            {
                targetMeshRenderer.materials = new Material[managedObject.nativeNumberOfMaterials];

                for (int currMaterialIndex = 0; currMaterialIndex < managedObject.nativeNumberOfMaterials; currMaterialIndex++)
                {
                    ManagedMaterial[] materials = managedObject.ManagedMaterials;
                    
                    targetMeshRenderer.materials[currMaterialIndex].shader = Shader.Find("Standard");

                    uint currentTextureIndex = 0;

                    Texture2D[] textures = null;
                    if (materials[currMaterialIndex].nativeTextureCount > 0)
                    {
                        textures = materials[currMaterialIndex].ManagedTextures;
                    }
                    
                    for (int propertyIndex = 0; propertyIndex < (int)NativeMaterialInfo.PropertyType.PROPERTYTYPE_COUNT; propertyIndex++)
                    {
                        ManagedMaterial.ManagedPropertyData[] propertyData = materials[currMaterialIndex].ManagedMaterialProperties;
                        if (propertyData[propertyIndex].ManagedTextureRelativeFileName != null ||
                            propertyData[propertyIndex].ManagedTextureAbsoluteFilePath != null)
                        {
                            Color color;
                            color.r = propertyData[propertyIndex].nativeDataColorValues.x;
                            color.g = propertyData[propertyIndex].nativeDataColorValues.y;
                            color.b = propertyData[propertyIndex].nativeDataColorValues.z;
                            color.a = propertyData[propertyIndex].nativeDataColorValues.w;

                            var currentMaterial = targetMeshRenderer.materials[currMaterialIndex];
                            switch (propertyData[propertyIndex].nativePropertyType)
                            {
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_EMISSIVE:
                                    {
                                        
                                        currentMaterial.EnableKeyword("_EMISSION");
                                        if (textures != null)
                                            currentMaterial.SetTexture(EmissionMap, textures[currentTextureIndex]);
                                        currentMaterial.SetColor(EmissionColor, color);
                                    }
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_AMBIENT:
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_DIFFUSE:
                                    {
                                        currentMaterial.EnableKeyword("_MainTex");
                                        if (textures != null)
                                            currentMaterial.SetTexture(MainTex, textures[currentTextureIndex]);
                                        currentMaterial.SetColor(MainTex, color);
                                    }
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_NORMAL:
                                    {
                                        currentMaterial.EnableKeyword("_BumpMap");
                                        if (textures != null)
                                            currentMaterial.SetTexture(BumpMap, textures[currentTextureIndex]);
                                        currentMaterial.SetFloat(BumpScale, color.a);
                                    }
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_BUMP:
                                    {
                                        currentMaterial.EnableKeyword("_BumpMap");
                                        if (textures != null)
                                            currentMaterial.SetTexture(BumpMap, textures[currentTextureIndex]);
                                        currentMaterial.SetFloat(BumpScale, color.a);
                                    }
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_TRANSPARENCY:
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_DISPLACEMENT:
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_VECTOR_DISPLACEMENT:
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_SPECULAR:
                                    {
                                        currentMaterial.EnableKeyword("_METALLICGLOSSMAP");
                                        if (textures != null)
                                            currentMaterial.SetTexture(MetallicGlossMap, textures[currentTextureIndex]);
                                        currentMaterial.SetFloat(Metallic, color.a);
                                    }
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_SHININESS:
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_REFLECTION:
                                    {
                                        currentMaterial.EnableKeyword("_Glossiness");
                                        currentMaterial.SetFloat(Smoothness, color.a);
                                    }
                                    break;
                                case NativeMaterialInfo.PropertyType.PROPERTYTYPE_COUNT:
                                    break;
                                default:
                                    break;
                            }

                            ++currentTextureIndex;
                        }
                    }
                }

                return true;
            }
             
            return false;
        }
        
        private void OnDestroy()
        {
            // Delete C++ handler which is unmanaged memory.
            PsychFbxUtilityDll.DestroyFBXHandler(unmanagedFbxHandler);

            unmanagedFbxHandler = IntPtr.Zero;
        }
        
        #endregion
    }
}
