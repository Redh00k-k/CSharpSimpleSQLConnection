﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using CommandLine;
using CommandLine.Text;

class Options {
    [Option('s', "server", Required = true, HelpText = "Server name.")]
    public string server { get; set; }

    [Option('d', "database", Required = true, HelpText = "Database name.")]
    public string database { get; set; }

    [Option('q', "query", Default = "", Required = false, HelpText = "Execute oneline query.")]
    public string one_query { get; set; }

    [Option('u', "user", Required = false, Default = "", HelpText = "User name.")]
    public string user { get; set; }

    [Option('p', "password", Required = false, Default = "", HelpText = "Password.")]
    public string password { get; set; }


    [Option('n', "no-password", Required = false, HelpText = "Don't ask for password")]
    public bool no_pass { get; set; }


    [Option('h', "help", Required = false, HelpText = "Display this help screen.")]
    public bool help { get; set; }
}


namespace SQL {
    class Program {
        static byte[] ObjectToByteArray(object obj) {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        static void ExecuteQueryAndShowResults(SqlConnection con, string query) {
            try {
                SqlCommand command = new SqlCommand(query, con);
                SqlDataReader reader = command.ExecuteReader();

                // Gets the values
                int row_num = 0;
                while (reader.Read()) {
                    Console.WriteLine($"----{row_num} row----");
                    for (int col = 0; col < reader.FieldCount; col++) {
                        reader.GetValue(col).GetType();
                        Console.Write(reader.GetName(col).ToString() + ": ");
                        if (reader.IsDBNull(col)) {
                            Console.WriteLine("NULL");
                        } else if (typeof(System.Byte[]) == reader.GetValue(col).GetType()) {
                            Console.WriteLine(BitConverter.ToString(ObjectToByteArray(reader.GetValue(col))).Replace("-", " ") + " <--- CyberChef 'From Hex'");
                        } else {
                            Console.WriteLine(reader.GetValue(col).ToString());
                        }
                    }
                    Console.WriteLine("");

                    row_num++;
                }
                reader.Close();
            } catch (Exception e) {
                Console.WriteLine($"Something wrong. {e.Message}");
            }
        }

        static void Run(Options opt) {
            String conString = "";
            if (opt.no_pass) {
                conString = "Server = " + opt.server + "; Database = " + opt.database + "; Integrated Security = True;";
            } else {
                string user = "";
                if (opt.user.Length != 0) { 
                    user = opt.user;
                    Console.WriteLine("User: " + user);
                } else {
                    Console.Write("User: ");
                    user = Console.ReadLine();
                }

                string password = "";
                if (opt.password.Length != 0) {
                    password = opt.password;
                    Console.WriteLine("Password: " + password);
                } else {
                    Console.Write("Password: ");
                    password = Console.ReadLine();
                }

                conString = "Server = " + opt.server + "; Database = " + opt.database + "; User ID = " + user + "; Password = " + password + ";";
            }
            Console.WriteLine(conString);

            SqlConnection con = new SqlConnection(conString);
            Console.WriteLine("Connecting ...");
            try {
                con.Open();
                Console.WriteLine("Auth success!");
            } catch {
                Console.WriteLine("Auth failed");
                Environment.Exit(0);
            }

            if (opt.one_query.Length != 0) {
                ExecuteQueryAndShowResults(con, opt.one_query);
                con.Close();

                return;
            }

            // send query and receive response.
            string query = "";
            do {
                Console.Write("Query > ");
                query = Console.ReadLine();
                if (query.ToLower() == "quit" || query.ToLower() == "exit") {
                    Console.WriteLine("Exiting ...");
                    break;
                }

                ExecuteQueryAndShowResults(con, query);

            } while (query != null) ;
            con.Close();

            return;
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs) {
            var helpText = HelpText.AutoBuild(result, h => {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "SQLConnection 0.1-beta";
                h.Copyright = "Copyright (c) Redh00k-k";
                h.AutoVersion = false;
                h.AutoHelp = false;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }


        static void Main(string[] args) {


            var parser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parseResult = parser.ParseArguments<Options>(args);

            parseResult.WithParsed<Options>(options => Run(options))
                        .WithNotParsed(errs => DisplayHelp(parseResult, errs));

            return;
        }
    }
}