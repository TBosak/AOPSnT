using AOPSnT.Interfaces;
using AOPSnT.Models;
using LiteDB;
using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Configuration;
using AOPSnT.Data;

namespace AOPSnT.Attributes
{
    public class Logging : OnMethodBoundaryAspect
    {
        private string dataFolder;
        private LiteDatabase mainDB;
        private string assemblyName;

        public Logging()
        {
            this.mainDB = GlobalConstants.Current.MainDB;
            this.dataFolder = GlobalConstants.Current.DataFolder;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnEntry(MethodExecutionArgs args)
        {
            assemblyName = Assembly.GetCallingAssembly().FullName.Split(",")[0];
            try
            {
                var parameters = new Dictionary<object, object>();
                foreach (var item in args.Method.GetParameters())
                {
                    parameters.Add(item.Name, args.Arguments[item.Position]);
                }
                var db = new LiteDatabase($"{dataFolder}{assemblyName}.db");
                var logMsg = new LogMessage()
                {
                    _id = args.Method.GetHashCode(),
                    Types = new List<string>() {
                    "OnEntry",
                    "Info"
                },
                    Instance = args.Instance,
                    Method = MethodName(args),
                    Parameters = parameters,
                    OnEntry = DateTime.Now
                };
                Log(db, logMsg);
            }
            catch (Exception e)
            {
                InternalError(args, e, "ONENTRY");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnExit(MethodExecutionArgs args)
        {
            assemblyName = Assembly.GetCallingAssembly().FullName.Split(",")[0];
            try
            {
                using (var db = new LiteDatabase($"{dataFolder}{assemblyName}.db"))
                {
                    var col = db.GetCollection<LogMessage>("Info");
                    var record = col.FindOne(x => x._id == args.Method.GetHashCode());
                    var onexitcol = db.GetCollection<LogMessage>("Info");
                    record.Types.Add("OnExit");
                    record.OnExit = DateTime.Now;
                    col.Update(record);

                    foreach (string type in record.Types)
                    {
                        var collection = db.GetCollection<ILogMessage>($"{type}");
                        bool update = collection.Update(record);
                        if (update) { continue; } else { collection.Insert(record); }
                    }
                }
            }
            catch(Exception e)
            {
                InternalError(args, e, "ONEXIT");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnException(MethodExecutionArgs args)
        {
            assemblyName = Assembly.GetCallingAssembly().FullName.Split(",")[0];
            try
            {
                using (var db = new LiteDatabase($"{dataFolder}{assemblyName}.db"))
                {
                    var col = db.GetCollection<LogMessage>("Info");
                    var record = col.FindOne(x => x._id == args.Method.GetHashCode());
                    var onexitcol = db.GetCollection<LogMessage>("Info");
                    Exception innerException = args.Exception.InnerException;
                    int layer = 0;
                    string exmsg = "";
                    while (!(innerException is null))
                    {
                        exmsg += $"TRACE LAYER({layer}): {innerException.Message}\n";
                        innerException = innerException.InnerException;
                        layer++;
                    }
                    record.Types.Add("OnException");
                    record.OnException = DateTime.Now;

                    if (exmsg.Length > 0)
                    {
                        record.Exception = exmsg;
                    }
                    else
                    {
                        record.Exception = "ERROR COULD NOT BE LOGGED";
                    }
                    foreach (string type in record.Types)
                    {
                        var collection = db.GetCollection<ILogMessage>($"{type}");
                        bool update = collection.Update(record);
                        if (update) { continue; } else { collection.Insert(record); }
                    }
                    onexitcol.Insert(record);
                }
            }
            catch (Exception e)
            {
                InternalError(args, e, "ONEXCEPTION");
            }
        }

        public string MethodName(MethodExecutionArgs args)
        {
            return $"{ args.Method.DeclaringType.FullName}.{ args.Method.Name} [{args.Arguments.Length}]";
        }

        public void InternalError(MethodExecutionArgs args, Exception e, string type)
        {
            var logMsg = new LogMessage()
            {
                Types = new List<string>() {
                            "InternalErrors"
                        },
                Instance = args.Instance,
                Method = MethodName(args),
                Exception = $"FAILED LOGGING {type} - INTERNAL ERROR: {e.Message}" + $"\n{e.StackTrace}"
            };
            Log(this.mainDB, logMsg);
        }

        public bool Log(ILiteDatabase db, ILogMessage logMessage)
        {
            try
            {
                using (db)
                {
                    foreach (string type in logMessage.Types)
                    {
                        var collection = db.GetCollection<ILogMessage>($"{type}");
                        collection.EnsureIndex(x => x._id);
                        collection.Insert(logMessage);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
