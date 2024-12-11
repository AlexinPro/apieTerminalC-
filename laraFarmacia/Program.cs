using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace laraFarmacia
{
    internal class Program
    {
        private static readonly string baseApiUrl = "http://127.0.0.1:8000/api/";
        private static readonly string token = "4GxK51a13IwonQMwGY9n61tLTmddBEdI0pr8na8W74260397";

        static void Main(string[] args)
        {
            ShowMenu().GetAwaiter().GetResult();
        }

        private static async Task ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Bienvenido a la API");
                Console.WriteLine("Seleccione una opción:");
                Console.WriteLine("1. Medicamentos");
                Console.WriteLine("2. Laboratorios");
                Console.WriteLine("3. Clientes");
                Console.WriteLine("4. Ventas");
                Console.WriteLine("5. Empleados");
                Console.WriteLine("0. Salir");
                Console.Write("Opción: ");

                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await ShowEntityMenu("medicamentos");
                        break;
                    case "2":
                        await ShowEntityMenu("laboratorios");
                        break;
                    case "3":
                        await ShowEntityMenu("clientes");
                        break;
                    case "4":
                        await ShowEntityMenu("ventas");
                        break;
                    case "5":
                        await ShowEntityMenu("empleados");
                        break;
                    case "0":
                        Console.WriteLine("Saliendo...");
                        return;
                    default:
                        Console.WriteLine("Por favor ingresa una opción válida.");
                        break;
                }

                Console.WriteLine("\nPresione cualquier tecla para volver al menú principal...");
                Console.ReadKey();
            }
        }

        private static async Task ShowEntityMenu(string endpoint)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Opciones para {endpoint}:");
                Console.WriteLine("1. Consultar (GET)");
                Console.WriteLine("2. Añadir (POST)");
                Console.WriteLine("3. Actualizar (PUT)");
                Console.WriteLine("4. Eliminar (DELETE)");
                Console.WriteLine("0. Volver al menú principal");
                Console.Write("Opción: ");

                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await GetData(endpoint);
                        break;
                    case "2":
                        await AddOrUpdateData(endpoint, isUpdate: false);
                        break;
                    case "3":
                        await AddOrUpdateData(endpoint, isUpdate: true);
                        break;
                    case "4":
                        await DeleteData(endpoint);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Por favor ingresa una opción válida.");
                        break;
                }

                Console.WriteLine("\nPresione cualquier tecla para continuar...");
                Console.ReadKey();
            }
        }

        private static async Task GetData(string endpoint)
        {
            string apiUrl = $"{baseApiUrl}{endpoint}/";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"\nDatos de {endpoint}:");

                        // Determinar si la respuesta es un array o un objeto
                        try
                        {
                            var jsonArray = JArray.Parse(jsonResponse);
                            foreach (var item in jsonArray)
                            {
                                Console.WriteLine("-------------------------");
                                foreach (var property in item.Children<JProperty>())
                                {
                                    Console.WriteLine($"{property.Name}: {property.Value}");
                                }
                            }
                        }
                        catch (JsonReaderException)
                        {
                            var jsonObject = JObject.Parse(jsonResponse);
                            var data = jsonObject["data"];

                            if (data != null)
                            {
                                foreach (var item in data)
                                {
                                    Console.WriteLine("-------------------------");
                                    foreach (var property in item.Children<JProperty>())
                                    {
                                        Console.WriteLine($"{property.Name}: {property.Value}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("No se encontraron datos.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error al obtener datos de {endpoint}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static async Task AddOrUpdateData(string endpoint, bool isUpdate)
        {
            Console.WriteLine($"\nFormulario para {(isUpdate ? "actualizar" : "añadir")} en {endpoint}:");

            object data = null;
            switch (endpoint)
            {
                case "medicamentos":
                    data = new
                    {
                        name = Prompt("Nombre"),
                        descripcion = Prompt("Descripción"),
                        caducidad = Prompt("Caducidad"),
                        precio = Prompt("Precio"),
                        laboratorio_id = Prompt("Laboratorio ID")
                    };
                    break;
                case "laboratorios":
                    data = new { laboratorio = Prompt("Laboratorio") };
                    break;
                case "empleados":
                    data = new
                    {
                        nombre = Prompt("Nombre"),
                        apellidos = Prompt("Apellidos"),
                        correo = Prompt("Correo")
                    };
                    break;
                case "clientes":
                    data = new
                    {
                        nombre = Prompt("Nombre"),
                        apellidos = Prompt("Apellidos"),
                        direccion = Prompt("Dirección")
                    };
                    break;
                case "ventas":
                    data = new
                    {
                        cliente_id = Prompt("Cliente ID"),
                        medicamento_id = Prompt("Medicamento ID"),
                        cantidad = Prompt("Cantidad")
                    };
                    break;
                default:
                    Console.WriteLine("Entidad no válida.");
                    return;
            }

            if (isUpdate)
            {
                Console.Write("Ingresa el ID del registro a actualizar: ");
                string id = Console.ReadLine();
                await SendRequest(endpoint, id, data, HttpMethod.Put);
            }
            else
            {
                await SendRequest(endpoint, null, data, HttpMethod.Post);
            }
        }

        private static async Task DeleteData(string endpoint)
        {
            Console.Write("Ingresa el ID del registro a eliminar: ");
            string id = Console.ReadLine();
            await SendRequest(endpoint, id, null, HttpMethod.Delete);
        }

        private static async Task SendRequest(string endpoint, string id, object data, HttpMethod method)
        {
            string apiUrl = id == null ? $"{baseApiUrl}{endpoint}/" : $"{baseApiUrl}{endpoint}/{id}/";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, apiUrl);

                    if (data != null)
                    {
                        string jsonData = JsonConvert.SerializeObject(data);
                        request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    }

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(method == HttpMethod.Post ? "Registro añadido exitosamente." : method == HttpMethod.Put ? "Registro actualizado exitosamente." : "Registro eliminado exitosamente.");
                    }
                    else
                    {
                        Console.WriteLine($"Error al procesar la solicitud: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static string Prompt(string field)
        {
            Console.Write($"{field}: ");
            return Console.ReadLine();
        }
    }
}
