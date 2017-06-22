using AntShares.Compiler;
using AntShares.Compiler.MSIL;
using Microsoft.Owin;
using remotegen;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace hhgate
{
    public class AntGateway : CustomServer.IParser
    {


        public async Task HandleRequest(IOwinContext context, string rootpath, string relativePath)
        {
            var api = relativePath.ToLower();
            var formdata = await FormData.FromRequest(context.Request);
            if (formdata == null)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                json["msg"] = new MyJson.JsonNode_ValueString("formdata format error.");
                json["tag"] = new MyJson.JsonNode_ValueNumber(-1);
                await context.Response.WriteAsync(json.ToString());
                return;
            }

            if (relativePath == "parse")
            {
                await parse(context, formdata);
                return;
            }

            else
            {
                await help(context, formdata);
                return;
            }


        }
        private static async Task help(IOwinContext context, FormData formdata)
        {
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            json["tag"] = new MyJson.JsonNode_ValueNumber(0);
            MyJson.JsonNode_Array maps = new MyJson.JsonNode_Array();
            json.SetDictValue("msg", "AntShares Http Gate By lights 0.01");
            await context.Response.WriteAsync(json.ToString());
            return;
        }

        public class Log2Json : ILogger
        {
            public MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();

            public void Log(string log)
            {
                array.Add(new MyJson.JsonNode_ValueString(log));
            }
        }
        private static async Task parse(IOwinContext context, FormData formdata)
        {

            if (formdata.mapParams.ContainsKey("language") && formdata.mapFiles.ContainsKey("file"))
            {
                try
                {
                    var file = formdata.mapFiles["file"];
                    var code = System.Text.Encoding.UTF8.GetString(file);

                    //编译
                    List<string> codes = new List<string>();
                    codes.Add(code);
                    CompilerResults r = null;

                    try
                    {
                        r = gencode.GenCode(codes, true);
                        if (r.Errors.Count > 0)
                        {
                            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                            json["tag"] = new MyJson.JsonNode_ValueNumber(-3);
                            json["msg"] = new MyJson.JsonNode_ValueString("compile fail.");
                            MyJson.JsonNode_Array errs = new MyJson.JsonNode_Array();
                            json["errors"] = errs;
                            for (var i = 0; i < r.Errors.Count; i++)
                            {
                                MyJson.JsonNode_Object obj = new MyJson.JsonNode_Object();
                                errs.Add(obj);
                                var _err = r.Errors[i];
                                obj.SetDictValue("ErrorText", _err.ErrorText);
                                obj.SetDictValue("ErrorNumber", _err.ErrorNumber);
                                obj.SetDictValue("IsWarning", _err.IsWarning);
                                obj.SetDictValue("Line", _err.Line);
                                obj.SetDictValue("Column", _err.Column);
                            }
                            await context.Response.WriteAsync(json.ToString());
                            return;
                        }
                    }
                    catch (Exception err)
                    {
                        MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                        json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
                        json["msg"] = new MyJson.JsonNode_ValueString("unknown fail on comp.");
                        json["err"] = new MyJson.JsonNode_ValueString(err.ToString());
                        await context.Response.WriteAsync(json.ToString());
                        return;
                    }
                    //conv
                    try
                    {
                        var st = System.IO.File.OpenRead(r.PathToAssembly);
                        using (st)
                        {
                            var logjson = new Log2Json();
                            var bs = Converter.Convert(st, logjson);
                            if (bs != null)
                            {

                                {
                                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                                    json["tag"] = new MyJson.JsonNode_ValueNumber(0);
                                    RIPEMD160Managed hash160 = new RIPEMD160Managed();
                                    var hash = hash160.ComputeHash(bs);
                                    StringBuilder sb2 = new StringBuilder();
                                    foreach(var b2 in bs)
                                    {
                                        sb2.Append(b2.ToString("X02"));
                                    }
                                    StringBuilder sb = new StringBuilder();
                                    foreach(var b in bs)
                                    {
                                        sb.Append(b.ToString("X02"));
                                    }
                                    json["AVMHexString"] = new MyJson.JsonNode_ValueString(sb.ToString());
                                    json["ScriptHash"] = new MyJson.JsonNode_ValueString(sb2.ToString());

                                    await context.Response.WriteAsync(json.ToString());
                                    return;
                                }
                            }
                            else
                            {

                                {
                                    MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                                    json["tag"] = new MyJson.JsonNode_ValueNumber(-4);
                                    json["msg"] = new MyJson.JsonNode_ValueString("compile fail.");
                                    json["info"]= logjson.array;
                                    await context.Response.WriteAsync(json.ToString());
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                        json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
                        json["msg"] = new MyJson.JsonNode_ValueString("unknown fail on conv.");
                        json["err"] = new MyJson.JsonNode_ValueString(err.ToString());
                        await context.Response.WriteAsync(json.ToString());
                        return;
                    }

                }
                catch
                {
                    {
                        MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                        json["tag"] = new MyJson.JsonNode_ValueNumber(-2);
                        json["msg"] = new MyJson.JsonNode_ValueString("parse fail.");
                        await context.Response.WriteAsync(json.ToString());
                        return;
                    }
                }


            }
            else
            {
                MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
                json["tag"] = new MyJson.JsonNode_ValueNumber(-1);
                json["msg"] = new MyJson.JsonNode_ValueString("need param: language & file.");
                await context.Response.WriteAsync(json.ToString());
                return;
            }

        }

    }

}
