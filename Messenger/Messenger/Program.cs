// A program that uses public key encryption to send secure messages to other users
// Author: Chen Lin

using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Messenger
{
    static class Program
    {
        private const string publicKeyFile = "public.key";
        private const string privateKeyFile = "private.key";
        private static BigInteger n;

        static void Main(String[] args)
        {
            if(args.Length < 2 || args.Length > 3)
            {
                commandLineHelp();
            }

            try
            {
                string option = args[0];
                string email = args[1];

                if (option == "keyGen")
                {
                    int size = int.Parse(args[1]);
                    RSAkeysGeneration(size);
                }

                else if (option == "sendKey")
                {
                    sendKey(email);
                }

                else if (option == "getKey")
                {
                    getKey(email);
                }

                else if (option == "sendMsg")
                {
                    if (string.IsNullOrEmpty(args[2]))
                    {
                        Console.WriteLine("No message entered");
                        return;
                    }
                    string plaintext = args[2];
                    sendMsg(email, plaintext);
                }

                else if (option == "getMsg")
                {
                    getMsg(email);
                }
            }

            catch (IndexOutOfRangeException) { };                                    

        }

        /*
         * Basic command line help
         */
        static void commandLineHelp()
        {
            Console.WriteLine("Invaild arguments\n" +
                "dotnet run <option> <other arguments>\n\n" +
                "Where options and other arguments can be\nkeyGen keysize\n" +
                "sendKey email\ngetKey email\n" +
                "sendMsg email plaintext\ngetMsg email\n");
        }

        /*
         * Use two prime numbers to generate RAS keys
         */
        public static void RSAkeysGeneration(int keySize)
        {
            Random random = new Random();
            
            int pSize = keySize / 2 - random.Next(20, 30);
            int qSize = keySize - pSize;

            BigInteger p = GeneratePrimeBigInteger(pSize);
            BigInteger q = GeneratePrimeBigInteger(qSize);

            n = p * q;
            BigInteger r = (p - 1) * (q - 1);
            BigInteger e = GeneratePrimeBigInteger(keySize / 4);
            BigInteger d = modInverse(e, r);

            var publicKeyBase64 = new PublicKey(" ", Convert.ToBase64String(d.ToByteArray()));
            var privateKeyBase64 = new PrivateKey(new List<string>(), Convert.ToBase64String(e.ToByteArray()));

            File.WriteAllText(publicKeyFile, JsonSerializer.Serialize(publicKeyBase64));
            File.WriteAllText(privateKeyFile, JsonSerializer.Serialize(privateKeyBase64));
            
        }

        /*
         * Sends the public key that was generated in the keyGen phase 
         * and send it to the server, with the email address given.
         */
        static void sendKey(string email)
        {
            try
            {
                if (!File.Exists(publicKeyFile))
                {
                    Console.WriteLine($"Key not found, generate it first.\n");

                }

                var publicKeyJson = File.ReadAllText(publicKeyFile);
                var publicKey = JsonSerializer.Deserialize<PublicKey>(publicKeyJson);

                publicKey.Email = email;

                HttpClient client = new HttpClient();

                var content = new StringContent(JsonSerializer.Serialize(publicKey), Encoding.UTF8, "application/json");
                var response = client.PutAsync($"http://kayrun.cs.rit.edu:5000/Key/{email}", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Key saved\n");
                }
                else
                {
                    Console.WriteLine($"Failed to send public key for {email}\n");
                }

                var privateKeyJson = File.ReadAllText(privateKeyFile);
                var privateKey = JsonSerializer.Deserialize<PrivateKey>(privateKeyJson);

                if (!privateKey.Emails.Contains(email))
                {
                    privateKey.Emails.Add(email);
                    File.WriteAllText(privateKeyFile, JsonSerializer.Serialize(privateKey));
                }

            }
            catch (FileNotFoundException)
            {

            }

        }

        /*
         * Retrieves the public key from the server for a given email address
         * and writes it to the file system, base64 encoded. 
         */
        static void getKey(string email)
        {

            HttpClient client = new HttpClient();
            var response = client.GetAsync($"http://kayrun.cs.rit.edu:5000/Key/{email}").Result;

            if (response.IsSuccessStatusCode)
            {
                var publicKeyJson = response.Content.ReadAsStringAsync().Result;
                var publicKey = JsonSerializer.Deserialize<PublicKey>(publicKeyJson);

                File.WriteAllText($"{email}.key", JsonSerializer.Serialize(publicKey));

                Console.WriteLine($"Public key for {email} retrieved and stored locally");

            }
            else
            {
                Console.WriteLine($"Failed to retrieve public key for {email}");

            }
        }

        /*
         * Send a message to the server view the correct command line
         * message. Message must be encrypted before sending it to the server
         */
        static void sendMsg(string email, string plaintext)
        {
            
            if (!File.Exists(publicKeyFile))
            {
                Console.WriteLine($"Public key for {email} not found, download it first.\n");
            }
           
            var publicKeyJson = File.ReadAllText(publicKeyFile);
            var publicKey = JsonSerializer.Deserialize<PublicKey>(publicKeyJson);

            var plaintextToBytes = Encoding.UTF8.GetBytes(plaintext);
            var plaintextToBigInt = new BigInteger(plaintextToBytes);
            var encryptedBigInt = BigInteger.ModPow(plaintextToBigInt, BigInteger.Parse(publicKey.Key), n); ;
            var encryptedBigIntToBytes = encryptedBigInt.ToByteArray();
            var encryptedBase64 = Convert.ToBase64String(encryptedBigIntToBytes);

            var message = new
            { Email = email,
              Message = encryptedBase64
            };

            var messageJson = JsonSerializer.Serialize(message);

            HttpClient client = new HttpClient();
            var content = new StringContent(messageJson, Encoding.UTF8, "application/json");
            var response = client.PutAsync($"http://kayrun.cs.rit.edu:5000/Message/{email}", content).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("\nMessage written\n");
            }

            else
            {
                Console.WriteLine("\nFail to write message\n");
            }

        }

        /*
         * Get an encrypted message from the server and decode it, using
         * the private key, and outputting it the console.
         */
        static void getMsg(string email)
        {

            if (!File.Exists(privateKeyFile))
            {
                Console.WriteLine("Private key not found.");
            }

            var privateKeyJson = File.ReadAllText(privateKeyFile);
            var privateKey = JsonSerializer.Deserialize<PrivateKey>(privateKeyJson);

            HttpClient client = new HttpClient();
            
            var response = client.GetAsync($"http://kayrun.cs.rit.edu:5000/Message/{email}").Result;
            response.EnsureSuccessStatusCode();

            var encryptedMessage = response.Content.ReadAsStringAsync().Result;
            var encryptedBytes = Convert.FromBase64String(encryptedMessage);
            var BytesToBigInt = new BigInteger(encryptedBytes);
            var DecryptedBigInt = BigInteger.ModPow(BytesToBigInt, BigInteger.Parse(privateKey.Key), n);
            var BigIntToBytes = DecryptedBigInt.ToByteArray();
            var plaintext = System.Text.Encoding.UTF8.GetString(BigIntToBytes); ;

            Console.WriteLine(plaintext);
            
        }

        /*
         * Generate one random prim big integer
         */
        static BigInteger GeneratePrimeBigInteger(int bits)
        {
            var bytes = new byte[bits];
            var rng = RandomNumberGenerator.Create();

            BigInteger PrimeBigInteger;
            do
            {               
                rng.GetBytes(bytes);

                PrimeBigInteger = new BigInteger(bytes);

            } while (!IsProbablyPrime(PrimeBigInteger));

            return PrimeBigInteger;
        }

        /*
         * Miller Rabin's primarily test
         */
        static Boolean IsProbablyPrime(this BigInteger value, int k = 10)
        {
            if (value < 2 || value % 2 == 0)
            {
                return false;
            }

            if (value == 2 || value == 3)
            {
                return true;
            }

            BigInteger d = value - 1;
            int count = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                count += 1;
            }

            byte[] bytes = new byte[value.ToByteArray().LongLength];
            var rng = RandomNumberGenerator.Create();

            BigInteger a = new BigInteger();

            for (int i = 0; i < k; i++)
            {

                while (a < 2 || value - 2 <= a)
                {
                    rng.GetBytes(bytes);
                    a = new BigInteger(bytes);
                }

                BigInteger x = BigInteger.ModPow(a, d, value);

                if (x == 1 || x == value - 1)
                {
                    continue;
                }

                for (int r = 0; r < count; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);

                    if (x == 1)
                    {
                        return false;
                    }

                    if (x == value - 1)
                    {
                        break;
                    }
                }

                if (x != value - 1)
                {
                    return false;
                }
            }
            return true;
        }

        /*
         * Helps generate public and private keys
         */
        static BigInteger modInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;

            while (a > 0)
            {
                BigInteger t = i / a;
                BigInteger x = a;

                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }

            v %= n;

            if (v < 0)
            {
                v = (v + n) % n;

            }
            return v;
        }
    }

    // Private key format
    public class PublicKey
    {
        public string Email { get; set; }
        public string Key { get; set; }

        public PublicKey(string email, string key)
        {
            Email = email;
            Key = key;          
        }
    }

    // Private key format
    public class PrivateKey
    {
        public List<string> Emails { get; set; }
        public string Key { get; set; }

        public PrivateKey(List<string> emails, string key)
        {
            Emails = emails;
            Key = key;
        }
    }

}

















