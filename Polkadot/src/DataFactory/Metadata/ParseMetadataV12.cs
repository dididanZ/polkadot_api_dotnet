﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Polkadot.DataStructs.Metadata;
using Polkadot.Source.Utils;

namespace Polkadot.DataFactory.Metadata
{
    public class ParseMetadataV12 : IParseFactory<MetadataV12>
    {
        public MetadataV12 Parse(JObject json)
        {
            var str = json["result"].ToString().Substring(2);
            // magic bytes
            var magic1 = Scale.NextByte(ref str);
            var magic2 = Scale.NextByte(ref str);
            var magic3 = Scale.NextByte(ref str);
            var magic4 = Scale.NextByte(ref str);
            var magic5 = Scale.NextByte(ref str);

            var result = new MetadataV12();

            var moduleList = new List<ModuleV12>();
            var mLen = Scale.DecodeCompactInteger(ref str);

            for (var moduleIndex = 0; moduleIndex < mLen.Value; moduleIndex++)
            {
                var module = new ModuleV12();

                // get module name
                var moduleNameLen = Scale.DecodeCompactInteger(ref str);
                module.Name = Scale.ExtractString(ref str, moduleNameLen.Value);

                // ---------- Storage
                // storage is not null
                var storageIsset = Scale.NextByte(ref str);
                if (storageIsset != 0)
                {
                    module.Storage = new StorageCollectionV12();
                    var storageList = new List<StorageV12>();

                    // get StorageCollection name
                    var storageNameLen = Scale.DecodeCompactInteger(ref str);
                    module.Storage.Prefix = Scale.ExtractString(ref str, storageNameLen.Value);

                    var storageLen = Scale.DecodeCompactInteger(ref str);
                    if (storageLen.Value == 0)
                    {
                        storageList.Add(new StorageV12());
                    }

                    for (int i = 0; i < storageLen.Value; i++)
                    {
                        storageList.Add(GetStorageV12(ref str));
                    }

                    module.Storage.Items = storageList.ToArray();
                }

                // ---------- Calls
                // calls is not null
                var callsIsset = Scale.NextByte(ref str);
                if (callsIsset != 0)
                {
                    var callList = new List<CallV12>();
                    var callsCount = Scale.DecodeCompactInteger(ref str);
                    if (callsCount.Value == 0)
                    {
                        callList.Add(new CallV12());
                    }

                    for (int i = 0; i < callsCount.Value; i++)
                    {
                        callList.Add(GetCallV12(ref str));
                    }
                    module.Call = callList.ToArray();
                }

                // ---------- Events
                // events is not null
                var eventsIsset = Scale.NextByte(ref str);
                if (eventsIsset != 0)
                {
                    var eventList = new List<EventArgV12>();
                    var eventsCount = Scale.DecodeCompactInteger(ref str);

                    if (eventsCount.Value == 0)
                    {
                        eventList.Add(new EventArgV12());
                    }

                    for (int i = 0; i < eventsCount.Value; i++)
                    {
                        eventList.Add(GetEventV12(ref str));
                    }

                    module.Ev = eventList.ToArray();
                }

                // ---------- Consts
                var constsCount = Scale.DecodeCompactInteger(ref str);
                var constsList = new List<ConstV12>();
                for (int i = 0; i < constsCount.Value; i++)
                {
                     constsList.Add(GetConstV12(ref str));
                }
                module.Cons = constsList.ToArray();

                // ---------- Errors
                var errorsCount = Scale.DecodeCompactInteger(ref str);
                var errorsList = new List<ErrorV12>();
                for (int i = 0; i < errorsCount.Value; i++)
                {
                    errorsList.Add(GetErrorV12(ref str));
                }
                module.Errors = errorsList.ToArray();
                module.Index = GetIndexV12(ref str);
                //index :0

                moduleList.Add(module);
            }
            result.Module = moduleList.ToArray();

            // get signed extensions
            var eLen = Scale.DecodeCompactInteger(ref str);
            for (var extrinsicIndex = 0; extrinsicIndex < eLen.Value; extrinsicIndex++)
            {
                var itemsLen = Scale.DecodeCompactInteger(ref str);
                var extrinsicExt = new List<string>();
                for (var itemIndex = 0; itemIndex < itemsLen.Value; itemIndex++)
                {
                    var nameLen = Scale.DecodeCompactInteger(ref str);
                    var name = Scale.ExtractString(ref str, nameLen.Value);
                    extrinsicExt.Add(name);
                }
                result.ExtrinsicExtension = extrinsicExt.ToArray();
            }

            if (str != string.Empty)
                throw new Exception("Wrong metadata version");

            return result;
        }

        private byte GetIndexV12(ref string str)
        {
            return Scale.NextByte(ref str);
        }
        private ErrorV12 GetErrorV12(ref string str)
        {
            var errors = new ErrorV12();

            // extract name
            var nameLen = Scale.DecodeCompactInteger(ref str);
            errors.Name = Scale.ExtractString(ref str, nameLen.Value);


            // documents count
            var docList = new List<string>();
            var docCount = Scale.DecodeCompactInteger(ref str);
            for (int di = 0; di < docCount.Value; di++)
            {
                var docStringLen = Scale.DecodeCompactInteger(ref str);
                var docItem = Scale.ExtractString(ref str, docStringLen.Value);
                docList.Add(docItem);
            }

            errors.Documentation = docList.ToArray();

            return errors;
        }

        private EventArgV12 GetEventV12(ref string str)
        {
            var ea = new EventArgV12();

            var callNameLen = Scale.DecodeCompactInteger(ref str);
            ea.Name = Scale.ExtractString(ref str, callNameLen.Value);

            // args count
            var argList = new List<string>();
            var args = Scale.DecodeCompactInteger(ref str);
            for(var i = 0; i < args.Value; i++)
            {
                var argLen = Scale.DecodeCompactInteger(ref str);
                argList.Add(Scale.ExtractString(ref str, argLen.Value));
            }
            ea.Args = argList.ToArray();

            // documents count
            var docList = new List<string>();
            var docCount = Scale.DecodeCompactInteger(ref str);
            for (var i = 0; i < docCount.Value; i++)
            {
                var docStringLen = Scale.DecodeCompactInteger(ref str);
                docList.Add(Scale.ExtractString(ref str, docStringLen.Value));
            }
            ea.Documentation = docList.ToArray();

            return ea;
        }

        private CallV12 GetCallV12(ref string str)
        {
            var call = new CallV12();

            var callNameLen = Scale.DecodeCompactInteger(ref str);
            call.Name = Scale.ExtractString(ref str, callNameLen.Value);

            // args count
            var argList = new List<FunctionCallArgV12>();
            var args = Scale.DecodeCompactInteger(ref str);
            for (var i = 0; i < args.Value; i++)
            {
                var fca = new FunctionCallArgV12();
                var argNameLen = Scale.DecodeCompactInteger(ref str);
                fca.Name = Scale.ExtractString(ref str, argNameLen.Value);

                var argTypeLen = Scale.DecodeCompactInteger(ref str);
                fca.Type = Scale.ExtractString(ref str, argTypeLen.Value);

                argList.Add(fca);
            }
            call.Args = argList.ToArray();

            // documents count
            var docList = new List<string>();
            var docCount = Scale.DecodeCompactInteger(ref str);
            for (var i = 0; i < docCount.Value; i++)
            {
                var docStringLen = Scale.DecodeCompactInteger(ref str);
                var docItem = Scale.ExtractString(ref str, docStringLen.Value);
                docList.Add(docItem);
            }
            call.Documentation = docList.ToArray();

            return call;
        }

        private StorageV12 GetStorageV12(ref string str)
        {
            var storage = new StorageV12();

            var storageNameLen = Scale.DecodeCompactInteger(ref str);
            storage.Name = Scale.ExtractString(ref str, storageNameLen.Value);

            storage.Modifier = Scale.NextByte(ref str);
            var hasSecondType = Scale.NextByte(ref str);

            storage.Type = new FuncTypeV12
            {
                Type = hasSecondType != 0 ? Scale.NextByte(ref str) : (uint)0
            };

            var type1Len = Scale.DecodeCompactInteger(ref str);
            var type1 = Scale.ExtractString(ref str, type1Len.Value);
            storage.Type.Key1 = type1;

            // map
            if (hasSecondType == 1)
            {
                // get value
                var valLen = Scale.DecodeCompactInteger(ref str);
                var value = Scale.ExtractString(ref str, valLen.Value);
                storage.Type.Value = value;
            }

            // double map
            if (hasSecondType == 2)
            {
                // get second key
                var type2Len = Scale.DecodeCompactInteger(ref str);
                var type2 = Scale.ExtractString(ref str, type2Len.Value);
                storage.Type.Value = type2;

                // get value
                var valLen = Scale.DecodeCompactInteger(ref str);
                var value = Scale.ExtractString(ref str, valLen.Value);
                storage.Type.Value = value;
            }

            if (hasSecondType != 0)
            {
                storage.Type.IsLinked = Scale.NextByte(ref str) != 0;
            }


            // extract fallback as raw hex
            var fallbackLen = Scale.DecodeCompactInteger(ref str);
            var fallback = str.Substring(0, (int)fallbackLen.Value * 2);
            str = str.Substring((int)fallbackLen.Value * 2);
            storage.Fallback = fallback;

            // documents count
            var docList = new List<string>();
            var docCount = Scale.DecodeCompactInteger(ref str);
            for (int di = 0; di < docCount.Value; di++)
            {
                var docStringLen = Scale.DecodeCompactInteger(ref str);
                var docItem = Scale.ExtractString(ref str, docStringLen.Value);
                docList.Add(docItem);
            }
            storage.Documentation = docList.ToArray();

            return storage;
        }

        private ConstV12 GetConstV12(ref string str)
        {
            var consts = new ConstV12();

            // extract name
            var nameLen = Scale.DecodeCompactInteger(ref str);
            consts.Name = Scale.ExtractString(ref str, nameLen.Value);

            // extract type
            var typeLen = Scale.DecodeCompactInteger(ref str);
            consts.Type = Scale.ExtractString(ref str, typeLen.Value);

            // extract value
            var valueLen = Scale.DecodeCompactInteger(ref str);
            var value = str.Substring(0, (int)valueLen.Value * 2);
            str = str.Substring((int)(valueLen.Value * 2));
            consts.Value = value;

            // documents count
            var docList = new List<string>();
            var docCount = Scale.DecodeCompactInteger(ref str);
            for (int di = 0; di < docCount.Value; di++)
            {
                var docStringLen = Scale.DecodeCompactInteger(ref str);
                var docItem = Scale.ExtractString(ref str, docStringLen.Value);
                docList.Add(docItem);
            }

            consts.Documentation = docList.ToArray();

            return consts;
        }
    }
}
