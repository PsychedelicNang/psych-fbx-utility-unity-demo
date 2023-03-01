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
        #region Member Variables
        private static CPPFBXHandler m_managedFBXHandler;
        static IntPtr unmanagedFBXHandler;

        MeshFilter          m_unityMeshFilter;
        Mesh                m_unityMesh;
        MeshRenderer        m_unityMeshRenderer;
        Transform           m_unityObjectTransform;
       
        public List<GameObject>    m_unityGameObjects;
        #endregion

        #region Class Methods
        void Start () {
            TimeSpan m_durationFileLoad;
            TimeSpan m_durationFileParsed;

            m_unityMeshFilter = gameObject.AddComponent<MeshFilter>();
            m_unityMesh = m_unityMeshFilter.mesh;
            m_unityMeshRenderer = gameObject.AddComponent<MeshRenderer>();
            m_unityObjectTransform = gameObject.GetComponent<Transform>();

            // C++ handler which is unmanaged memory (we need to delete by calling DLLDestroyFBXHandler())
            unmanagedFBXHandler = PsychFbxUtilityDll.CreateFBXHandler();

            DateTime m_timeBeforeFileLoad = DateTime.Now;

            var fbxPath = "../Submodules/psych-fbx-utility/psych-fbx-utility-vs/resources/SM_PistolArnold.fbx";

            CRESULT result = (CRESULT)PsychFbxUtilityDll.LoadFBXFile(unmanagedFBXHandler, fbxPath);
		    DateTime m_timeAfterFileLoad = DateTime.Now;
		    print (result);
            m_durationFileLoad = m_timeAfterFileLoad - m_timeBeforeFileLoad;

            switch (result)
            {
                case CRESULT.CRESULT_SUCCESS:
                    {
                        DateTime m_timeBeforeFileParsed = DateTime.Now;
                        CSParseFBXHandler();
                        DateTime m_timeAfterFileParsed = DateTime.Now;

                        m_durationFileParsed = m_timeAfterFileParsed - m_timeBeforeFileParsed;

                        if (m_durationFileLoad.Seconds > 0)
                            print("The FBX File loaded in: " + m_durationFileLoad.Seconds + "." + m_durationFileLoad.Milliseconds + " s");
                        else
                            print("The FBX File loaded in: " + m_durationFileLoad.Milliseconds + " ms");

                        if (m_durationFileParsed.Seconds > 0)
                            print("The FBX File was parsed in: " + m_durationFileParsed.Seconds + "." + m_durationFileParsed.Milliseconds + " s");
                        else
                            print("The FBX File was parsed in: " + m_durationFileParsed.Milliseconds + " ms");
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

            m_unityObjectTransform.localScale = new Vector3(10.0f, 10.0f, 10.0f);
            m_unityObjectTransform.Translate(new Vector3(0.0f, 3.0f, 0.0f));
        }

        void CSParseFBXHandler()
        {
            m_managedFBXHandler = (CPPFBXHandler)Marshal.PtrToStructure(unmanagedFBXHandler, typeof(CPPFBXHandler));

            m_unityGameObjects = new List<GameObject>((int)m_managedFBXHandler.CPPFBXScene.m_numberOfObjects);

            CPPObject[] sceneObjects = m_managedFBXHandler.CPPFBXScene.CPPObjects;

            for (uint currObjectIndex = 0;
                 currObjectIndex < (int)m_managedFBXHandler.CPPFBXScene.m_numberOfObjects;
                 currObjectIndex++)
            {
                GameObject unityGameObject = new GameObject();
                m_unityGameObjects.Add(unityGameObject);

                MeshFilter currMeshFilter = unityGameObject.AddComponent<MeshFilter>();
                MeshRenderer currMeshRenderer = unityGameObject.AddComponent<MeshRenderer>();

                unityGameObject.name = sceneObjects[currObjectIndex].Name;

                print("Object: " + sceneObjects[currObjectIndex].Name + "\tMaterial Count: " + sceneObjects[currObjectIndex].m_numberOfMaterials + "\tNumber of Children: " + sceneObjects[currObjectIndex].m_numberOfChildren);

                // Mesh info
                if (sceneObjects[currObjectIndex].CPPMesh.m_vertexCount > 0)
                {
                    CMath.Vector3[] vertices = sceneObjects[currObjectIndex].CPPMesh.VertexPositions;
                    CMath.Vector3[] normals = sceneObjects[currObjectIndex].CPPMesh.Normals;
                    CMath.Vector2[] uvs = sceneObjects[currObjectIndex].CPPMesh.UVs;

                    Vector3[] unityVerts = new Vector3[sceneObjects[currObjectIndex].CPPMesh.m_vertexCount];
                    Vector3[] unityNormals = new Vector3[sceneObjects[currObjectIndex].CPPMesh.m_vertexCount];
                    Vector2[] unityUVs = new Vector2[sceneObjects[currObjectIndex].CPPMesh.m_vertexCount];
                
                    for (uint currVertex = 0; currVertex < sceneObjects[currObjectIndex].CPPMesh.m_vertexCount; currVertex++)
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

                    var mesh = currMeshFilter.mesh;
                    mesh.vertices = unityVerts;
                    mesh.normals = unityNormals;
                    mesh.uv = unityUVs;
                    mesh.triangles = sceneObjects[currObjectIndex].CPPMesh.ConvertedIndices;
                    
                }
                
                // Material info
                if (sceneObjects[currObjectIndex].m_numberOfMaterials > 0)
                {
                    currMeshRenderer.materials = new Material[sceneObjects[currObjectIndex].m_numberOfMaterials];

                    for (int currMaterialIndex = 0; currMaterialIndex < sceneObjects[currObjectIndex].m_numberOfMaterials; currMaterialIndex++)
                    {
                        CPPMaterial[] materials = sceneObjects[currObjectIndex].CPPMaterials;
                        
                        currMeshRenderer.materials[currMaterialIndex].shader = Shader.Find("Standard");

                        uint currentTextureIndex = 0;

                        Texture2D[] textures = null;
                        if (materials[currMaterialIndex].m_textureCount > 0)
                        {
                            textures = materials[currMaterialIndex].Textures;
                        }
                        
                        for (int propertyIndex = 0; propertyIndex < (int)CMaterialInfo.PropertyType.PROPERTYTYPE_COUNT; propertyIndex++)
                        {
                            CPPMaterial.CPPPropertyData[] propertyData = materials[currMaterialIndex].MaterialProperties;
                            if (propertyData[propertyIndex].TextureRelativeFileName != null ||
                                propertyData[propertyIndex].TextureAbsoluteFilePath != null)
                            {
                                Color color;
                                color.r = propertyData[propertyIndex].m_dataColorValues.x;
                                color.g = propertyData[propertyIndex].m_dataColorValues.y;
                                color.b = propertyData[propertyIndex].m_dataColorValues.z;
                                color.a = propertyData[propertyIndex].m_dataColorValues.w;
                                
                                switch (propertyData[propertyIndex].m_propertyType)
                                {
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_EMISSIVE:
                                        {
                                            
                                            currMeshRenderer.materials[currMaterialIndex].EnableKeyword("_EMISSION");
                                            currMeshRenderer.materials[currMaterialIndex].SetTexture("_EmissionMap", textures[currentTextureIndex]);
                                            currMeshRenderer.materials[currMaterialIndex].SetColor("_EmissionColor", color);
                                        }
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_AMBIENT:
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_DIFFUSE:
                                        {
                                            currMeshRenderer.materials[currMaterialIndex].EnableKeyword("_MainTex");
                                            currMeshRenderer.materials[currMaterialIndex].SetTexture("_MainTex", textures[currentTextureIndex]);
                                            currMeshRenderer.materials[currMaterialIndex].SetColor("_MainTex", color);
                                        }
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_NORMAL:
                                        {
                                            currMeshRenderer.materials[currMaterialIndex].EnableKeyword("_BumpMap");
                                            currMeshRenderer.materials[currMaterialIndex].SetTexture("_BumpMap", textures[currentTextureIndex]);
                                            currMeshRenderer.materials[currMaterialIndex].SetFloat("_BumpScale", color.a);
                                        }
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_BUMP:
                                        {
                                            currMeshRenderer.materials[currMaterialIndex].EnableKeyword("_BumpMap");
                                            currMeshRenderer.materials[currMaterialIndex].SetTexture("_BumpMap", textures[currentTextureIndex]);
                                            currMeshRenderer.materials[currMaterialIndex].SetFloat("_BumpScale", color.a);
                                        }
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_TRANSPARENCY:
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_DISPLACEMENT:
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_VECTOR_DISPLACEMENT:
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_SPECULAR:
                                        {
                                            currMeshRenderer.materials[currMaterialIndex].EnableKeyword("_METALLICGLOSSMAP");
                                            currMeshRenderer.materials[currMaterialIndex].SetTexture("_MetallicGlossMap", textures[currentTextureIndex]);
                                            currMeshRenderer.materials[currMaterialIndex].SetFloat("Metallic", color.a);
                                        }
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_SHININESS:
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_REFLECTION:
                                        {
                                            currMeshRenderer.materials[currMaterialIndex].EnableKeyword("_Glossiness");
                                            currMeshRenderer.materials[currMaterialIndex].SetFloat("Smoothness", color.a);
                                        }
                                        break;
                                    case CMaterialInfo.PropertyType.PROPERTYTYPE_COUNT:
                                        break;
                                    default:
                                        break;
                                }

                                ++currentTextureIndex;
                            }
                        }
                    }
                }
            }
            
            for (uint currObjectIndex = 0; currObjectIndex < m_managedFBXHandler.CPPFBXScene.m_numberOfObjects; currObjectIndex++)
            {
                m_unityGameObjects[(int)currObjectIndex].transform.parent = m_unityGameObjects[(int)sceneObjects[currObjectIndex].m_parentArrayIndexID].transform;
            }

            m_unityGameObjects[0].transform.parent = m_unityObjectTransform;
        }
        
        private void OnDestroy()
        {
            // Delete C++ handler which is unmanaged memory.
            PsychFbxUtilityDll.DestroyFBXHandler(unmanagedFBXHandler);

            unmanagedFBXHandler = IntPtr.Zero;
        }
        #endregion
    }
}
