﻿/*
 * DescribeSettingDefinition
 * (c) Stefan Licht, 2010
 */

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sones.GraphDB.Structures.EdgeTypes;
using sones.Lib.ErrorHandling;
using sones.GraphDB.Errors;
using sones.GraphDB.Functions;
using sones.GraphDB.TypeManagement;
using sones.GraphDB.Indices;
using sones.GraphDB.Exceptions;
using sones.GraphDB.Settings;
using sones.GraphDB.Structures.Enums;
using sones.GraphFS.Session;
using sones.GraphDB.Result;
using sones.GraphDB.NewAPI;

#endregion

namespace sones.GraphDB.Managers.Structures.Describe
{
    /// <summary>
    /// Describes a setting
    /// </summary>
    public class DescribeSettingDefinition : ADescribeDefinition
    {

        #region Data

        /// <summary>
        /// The settings scope
        /// </summary>
        private TypesSettingScope? _SettingType;

        /// <summary>
        /// The setting name
        /// </summary>
        private String _SettingName;

        /// <summary>
        /// The type name of the setting
        /// </summary>
        private String _SettingTypeName;

        /// <summary>
        /// The chain definition of the setting
        /// </summary>
        private IDChainDefinition _SettingAttribute;

        #endregion

        #region Ctor

        public DescribeSettingDefinition() { }

        public DescribeSettingDefinition(TypesSettingScope? mySettingType, String mySettingName = null, String myTypeName = null, IDChainDefinition myIDChain = null)
        {
            _SettingType = mySettingType;
            _SettingName = mySettingName;
            _SettingTypeName = myTypeName;
            _SettingAttribute = myIDChain;
        }

        #endregion

        #region ADescribeDefinition

        /// <summary>
        /// <seealso cref=" ADescribeDefinition"/>
        /// </summary>
        public override Exceptional<IEnumerable<Vertex>> GetResult(DBContext myDBContext)
        {

            var _ResultingVertices = new List<Vertex>();

            if (_SettingType.HasValue)
            {

                #region Special _SettingType

                switch (_SettingType.Value)
                {
                    case TypesSettingScope.TYPE:

                        #region TYPE

                        var type = myDBContext.DBTypeManager.GetTypeByName(_SettingTypeName);
                        if (type == null)
                        {
                            return new Exceptional<IEnumerable<Vertex>>(new Error_TypeDoesNotExist(_SettingTypeName));
                        }
                        if (String.IsNullOrEmpty(_SettingName))
                        {
                            var typeResult = GenerateTypeResult(type, myDBContext);
                            if (typeResult.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(typeResult);
                            }

                            _ResultingVertices.AddRange(typeResult.Value);
                        }
                        else
                        {
                            var descrTypeRes = GenerateTypeResult(_SettingName, type, myDBContext);
                            if (descrTypeRes.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(descrTypeRes);
                            }
                            _ResultingVertices.AddRange(descrTypeRes.Value);
                        }
                        break;

                        #endregion

                    case TypesSettingScope.ATTRIBUTE:

                        #region ATTRIBUTE

                        Exceptional validateResult = _SettingAttribute.Validate(myDBContext, false);
                        if (validateResult.Failed())
                        {
                            return new Exceptional<IEnumerable<Vertex>>(validateResult.IErrors);
                        }

                        if (String.IsNullOrEmpty(_SettingName))
                        {
                            var attributeResult = GenerateAttrResult(_SettingAttribute.LastAttribute, myDBContext);
                            if (attributeResult.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(attributeResult);
                            }
                            _ResultingVertices.AddRange(attributeResult.Value);
                        }
                        else
                        {
                            var outputResult = GenerateAttrResult(_SettingName, _SettingAttribute.LastAttribute, myDBContext);

                            if (outputResult.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(outputResult);
                            }
                            _ResultingVertices.Add(outputResult.Value);
                        }
                        break;

                        #endregion

                    case TypesSettingScope.SESSION:

                        #region SESSION

                        if (String.IsNullOrEmpty(_SettingName))
                        {
                            var sessionReadouts = GenerateSessionResult(myDBContext);
                            if (sessionReadouts.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(sessionReadouts);
                            }
                            _ResultingVertices.AddRange(sessionReadouts.Value);
                        }
                        else
                        {
                            var outputResult = GenerateSessionResult(_SettingName, myDBContext);

                            if (outputResult.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(outputResult);
                            }
                            _ResultingVertices.Add(outputResult.Value);
                        }
                        break;

                        #endregion

                    case TypesSettingScope.DB:

                        #region DB

                        if (String.IsNullOrEmpty(_SettingName))
                        {
                            var dbResult = GenerateDBResult(myDBContext);
                            if (dbResult.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(dbResult);
                            }
                            _ResultingVertices.AddRange(dbResult.Value);
                        }
                        else
                        {
                            var outputResult = GenerateDBResult(_SettingName, myDBContext);
                            if (outputResult.Failed())
                            {
                                return new Exceptional<IEnumerable<Vertex>>(outputResult);
                            }
                            _ResultingVertices.Add(outputResult.Value);
                        }

                        #endregion

                        break;
                    default:

                        return new Exceptional<IEnumerable<Vertex>>(new Error_NotImplemented(new System.Diagnostics.StackTrace()));
                }

                #endregion

            }

            else
            {

                #region No SettingType

                if (String.IsNullOrEmpty(_SettingName))
                {

                    #region Describe all settings

                    var readOutList = new List<Vertex>();
                    foreach (var Item in myDBContext.DBSettingsManager.GetAllSettings())
                    {
                        readOutList.Add(GenerateResult(Item, myDBContext.DBTypeManager));
                    }
                    _ResultingVertices.AddRange(readOutList);

                    #endregion

                }
                else
                {

                    #region Describe named setting

                    var outputResult = GenerateStdResult(_SettingName, myDBContext);

                    if (outputResult.Failed())
                    {
                        return new Exceptional<IEnumerable<Vertex>>(outputResult);
                    }

                    _ResultingVertices.Add(outputResult.Value);

                    #endregion

                }

                #endregion

            }

            return new Exceptional<IEnumerable<Vertex>>(_ResultingVertices);

        }
        
        #endregion

        #region Output

        #region Standard output value

        /// <summary>
        /// generate a output result if no database, session, type or attribute is requested, then you can get information about the setting
        /// </summary>
        /// <param name="mySettingName">The name of the setting</param>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<Vertex> GenerateStdResult(string mySettingName, DBContext myDBContext)
        {

            var settingResult = myDBContext.DBSettingsManager.GetSetting(mySettingName);
            if (settingResult.Failed())
            {
                return new Exceptional<Vertex>(settingResult);
            }
            else
            {
                return new Exceptional<Vertex>(GenerateResult(settingResult.Value, myDBContext.DBTypeManager));
            }


        }

        #endregion

        #region output result for a attribute

        /// <summary>
        /// Generate a output result for setting on a attribute
        /// </summary>
        /// <param name="mySettingName">The name of the setting</param>
        /// <param name="myTypeNode">The typenode</param>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<Vertex> GenerateAttrResult(string mySettingName, TypeAttribute myAttribute, DBContext myDBContext)
        {
            var setting = myDBContext.DBSettingsManager.GetSetting(mySettingName, myDBContext, TypesSettingScope.ATTRIBUTE, myAttribute.GetRelatedType(myDBContext.DBTypeManager), myAttribute);
            if (setting.Failed())
            {
                return new Exceptional<Vertex>(setting);
            }

            return new Exceptional<Vertex>(GenerateResult(setting.Value, myDBContext.DBTypeManager));
        }

        /// <summary>
        /// Generate a output result for setting on a attribute
        /// </summary>
        /// <param name="mySettingName">the name of the setting</param>
        /// <param name="myTypeNode">typenode</param>
        /// <param name="mySessionToken"></param>
        private Exceptional<List<Vertex>> GenerateAttrResult(TypeAttribute myAttribute, DBContext myDBContext)
        {
            List<Vertex> resultingObjects = new List<Vertex>();

            var settings = myDBContext.DBSettingsManager.GetAllSettings(myDBContext, TypesSettingScope.ATTRIBUTE, myAttribute.GetRelatedType(myDBContext.DBTypeManager), myAttribute);
            foreach (var setting in settings)
            {
                resultingObjects.Add(GenerateResult(setting.Value, myDBContext.DBTypeManager));
            }

            return new Exceptional<List<Vertex>>(resultingObjects);
        }

        #endregion

        #region generate a output result for a db setting
        
        /// <summary>
        /// Generate a output result for a database setting
        /// </summary>
        /// <param name="mySettingName">The name of the setting</param>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<Vertex> GenerateDBResult(String mySettingName, DBContext myDBContext)
        {
            var setting = myDBContext.DBSettingsManager.GetSetting(mySettingName, myDBContext, TypesSettingScope.DB, includingDefaults: false);
            if (setting.Failed())
            {
                return new Exceptional<Vertex>(setting);
            }

            return new Exceptional<Vertex>(GenerateResult(setting.Value, myDBContext.DBTypeManager));
        }

        /// <summary>
        /// Generate a output result for a database setting
        /// </summary>
        /// <param name="myDBContext"></param>
        private Exceptional<List<Vertex>> GenerateDBResult(DBContext myDBContext)
        {
            List<Vertex> resultingReadouts = new List<Vertex>();

            foreach (var aDBSetting in myDBContext.DBSettingsManager.GetAllSettings(myDBContext, TypesSettingScope.DB, includingDefaults: false))
            {
                resultingReadouts.Add(GenerateResult(aDBSetting.Value, myDBContext.DBTypeManager));
            }

            return new Exceptional<List<Vertex>>(resultingReadouts);

        }

        #endregion

        #region generate a output result for a session setting

        /// <summary>
        /// Generate a output result for a session setting
        /// </summary>
        /// <param name="mySettingName">The name of the setting</param>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<Vertex> GenerateSessionResult(string mySettingName, DBContext myDBContext)
        {
            var setting = myDBContext.DBSettingsManager.GetSetting(mySettingName, myDBContext, TypesSettingScope.SESSION);
            if (setting.Failed())
            {
                return new Exceptional<Vertex>(setting);
            }

            return new Exceptional<Vertex>(GenerateResult(setting.Value, myDBContext.DBTypeManager));
        }

        /// <summary>
        /// Generate a output result for a session setting
        /// </summary>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<List<Vertex>> GenerateSessionResult(DBContext myDBContext)
        {
            List<Vertex> resultingReadouts = new List<Vertex>();

            foreach (var Setting in myDBContext.DBSettingsManager.GetAllSettings(myDBContext, TypesSettingScope.SESSION, null, null, false))
            {
                resultingReadouts.Add(GenerateResult(Setting.Value, myDBContext.DBTypeManager));
            }
            return new Exceptional<List<Vertex>>(resultingReadouts);
        }

        #endregion  

        #region output for a type

        /// <summary>
        /// Generate a output result for settings on a type
        /// </summary>
        /// <param name="myType">The type</param>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<List<Vertex>> GenerateTypeResult(GraphDBType myType, DBContext myDBContext)
        {
            List<Vertex> resultingReadouts = new List<Vertex>();

            foreach (var Setting in myDBContext.DBSettingsManager.GetAllSettings(myDBContext, TypesSettingScope.TYPE, myType))
            {
                resultingReadouts.Add(GenerateResult(Setting.Value, myDBContext.DBTypeManager));
            }

            return new Exceptional<List<Vertex>>(resultingReadouts);
        }

        /// <summary>
        /// generate a output result for settings on a type
        /// </summary>
        /// <param name="myType">The type</param>
        /// <param name="myDBContext">The db context</param>
        private Exceptional<List<Vertex>> GenerateTypeResult(string mySettingName, GraphDBType myType, DBContext myDBContext)
        {

            var Setting = myDBContext.DBSettingsManager.GetSetting(mySettingName, myDBContext, TypesSettingScope.TYPE, myType);
            if (Setting.Failed())
            {
                return new Exceptional<List<Vertex>>(Setting);
            }

            return new Exceptional<List<Vertex>>(new List<Vertex>() { GenerateResult(Setting.Value, myDBContext.DBTypeManager) });

        }

        #endregion

        /// <summary>
        /// Generate a output result
        /// </summary>
        /// <param name="mySetting">The setting</param>
        /// <param name="myTypeManager">The db type manager</param>
        private Vertex GenerateResult(ADBSettingsBase mySetting, DBTypeManager myTypeManager)
        {

            var Setting = new Dictionary<String, Object>();
            Setting.Add("Name", mySetting.Name);
            Setting.Add("ID", mySetting.ID);
            Setting.Add("Type", myTypeManager.GetTypeByUUID(mySetting.Type).Name);
            Setting.Add("Desc", mySetting.Description);
            Setting.Add("Default", mySetting.Default);
            Setting.Add("Value", mySetting.Value);

            return new Vertex(Setting);

        }

        #endregion

    }
}
