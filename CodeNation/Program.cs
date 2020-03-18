using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeNation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            using (var client = new HttpClient())
            {
                string url = "https://api.codenation.dev/v1/challenge/dev-ps/generate-data?token=a0925d87fc3e512c0ca83b8b144364e5796c927c";
                var response = await client.GetAsync(url);
                var resultGet = await response.Content.ReadAsStringAsync();
                var objeto = JsonConvert.DeserializeObject<Answer>(resultGet);

                string decode = Decode(objeto.cifrado, objeto.numero_casas);
                objeto.decifrado = decode;
                objeto.resumo_criptografico = CalculateSHA1(decode, Encoding.UTF8).ToLower();

                Console.WriteLine(JsonConvert.SerializeObject(objeto));

                var arquivo = CriarArquivo(JsonConvert.SerializeObject(objeto));
                var body = JsonConvert.SerializeObject(new { answer = arquivo });
                using (MultipartFormDataContent httpContent = new MultipartFormDataContent())
                using (StreamContent fileContent = new StreamContent(new MemoryStream(arquivo)))
                {
                    httpContent.Add(fileContent, "answer", "answer.json");
                    var message = await client.PostAsync("https://api.codenation.dev/v1/challenge/dev-ps/submit-solution?token=a0925d87fc3e512c0ca83b8b144364e5796c927c", httpContent);
                    var input = await message.Content.ReadAsStringAsync();
                    Console.WriteLine(input);
                }

                Console.WriteLine("");
                Console.ReadKey();
            }
        }

        public static string CalculateSHA1(string text, Encoding enc)
        {
            try
            {
                byte[] buffer = enc.GetBytes(text);
                System.Security.Cryptography.SHA1CryptoServiceProvider cryptoTransformSHA1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                string hash = BitConverter.ToString(cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");
                return hash;
            }
            catch (Exception x)
            {
                throw new Exception(x.Message);
            }
        }

        private static string Decode(string decode, int salto)
        {
            //Regex regex = new Regex(@"/^[a-zA-Z]*$/");
            //Regex regex = new Regex((@"a-zA-Z+"));

            StringBuilder result = new StringBuilder();
            foreach (char letra in decode)
            {
                byte[] ascii = Encoding.ASCII.GetBytes(letra.ToString());
                int encodeInt = int.Parse(ascii[0].ToString());
                if ((encodeInt >= 97) && (encodeInt <= 122))
                {
                    int encodeNumber = ascii[0] - salto;

                    if (encodeNumber < 97)
                    {
                        encodeNumber = 123 - (encodeNumber - 97) * -1;
                        char character = (char)encodeNumber;
                        result.Append(character);
                    }
                    else
                    {
                        char character = (char)encodeNumber;
                        result.Append(character);
                    }
                }
                else
                {
                    result.Append(letra.ToString());
                }
            }

            return result.ToString();
        }

        private static byte[] CriarArquivo(string content)
        {

            string nomeArquivo = Directory.GetCurrentDirectory() + "/answer.json";
            FileStream fs = new FileStream(nomeArquivo, FileMode.Create);
            StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
            
            writer.WriteLine(content);

            writer.Flush();
            writer.Close();
            writer.Dispose();
            fs.Close();
            fs.Dispose();

            Thread.Sleep(3);

            return File.ReadAllBytes(nomeArquivo);
        }
    }
}
