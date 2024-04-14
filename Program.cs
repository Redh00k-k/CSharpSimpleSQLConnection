using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using CommandLine;
using CommandLine.Text;

class Options {
    [Option('s', "server", Required = true, HelpText = "Server name.")]
    public string server { get; set; }

    [Option('d', "database", Required = true, HelpText = "Database name.")]
    public string database { get; set; }

    [Option('n', "no-password", Required = false, HelpText = "Don't ask for password")]
    public bool no_pass { get; set; }

    [Option('h', "help", Required = false, HelpText = "Display this help screen.")]
    public bool help { get; set; }
}


namespace SQL {
    class Program {
        static void Run(Options opt) {
            Console.Write("User: ");
            string user = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            String conString = "";
            if (opt.no_pass) {
                conString = "Server = " + opt.server + "; Database = " + opt.database + "; Integrated Security = True;";
            } else {
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

            // send query and receive response.
            string line = "";
            try {
                do {
                    Console.Write("Query > ");
                    string query = Console.ReadLine();
                    if (line != null) {
                        SqlCommand command = new SqlCommand(query, con);
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read()) {
                            Console.WriteLine("" + reader[0]);
                        }
                        reader.Close();
                    }
                } while (line != null);
            } catch (Exception e) {
                Console.WriteLine($"Something wrong. {e.Message}");
            } finally {
                con.Close();
            }
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