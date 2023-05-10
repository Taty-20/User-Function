using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using User_Function.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace User_Function /*Nombre del Namespace*/
{
    public static class FunctionCRUD /*Declaración de la clase estática*/
    {
        [FunctionName(nameof(AuthenticationUser))]
        public static async Task<IActionResult> AuthenticationUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth")] UserCredentials userCredentials, HttpRequest req, ILogger log)
        {
            //Autenticación simple de Ejemplo:
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<UserCredentials>(requestBody);
            using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
            {
                connection.Open();

                var query = $"SELECT COUNT (*) FROM [Usuario] WHERE Correo='{input.User}' AND Contraseña = '{input.Password}'";

                SqlCommand command = new SqlCommand(query, connection);
                

                command.ExecuteNonQuery();
                int count = (int)await command.ExecuteScalarAsync();
                if (count == 0)
                {
                    return await Task.FromResult(new UnauthorizedResult()).ConfigureAwait(false);
                }
                else
                {
                    GenerateJWTToken generateJWTToken = new();
                    string token = generateJWTToken.IssuingJWT(userCredentials.User);
                    return await Task.FromResult(new OkObjectResult(token)).ConfigureAwait(false);
                }
            }
            
        } 

        [FunctionName(nameof(GetToken))]
        public static async Task<IActionResult> GetToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "token")] HttpRequest req,
            ILogger log)
        {
            //Autenticación simple de Ejemplo:
            ValidateJWT auth = new ValidateJWT(req);

            if(!auth.IsValid)
            {
                return new UnauthorizedResult();
            }
            string postData = await req.ReadAsStringAsync();
            return new OkObjectResult($"{postData}");
            

        }
        
        [FunctionName("CreateUser")] /*Nombre de la Function*/
        public static async Task<IActionResult> CreateUser(
            /*Se declara la Function como método POST en la ruta USER*/ /*Por ejemplo: http://www.ejemplo.com/api/user/*/
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")] HttpRequest req,
            ILogger log)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<CreateUserModel>(requestBody);
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    /*Abre la conexión*/
                    connection.Open();

                    /*Declara Query*/
                    var query = $"INSERT INTO [Usuario] (Nombre, Apellido, Correo, Contraseña, Rol) VALUES('{input.Nombre}', '{input.Apellido}', '{input.Correo}', '{input.Contraseña}' ,'1')";
                    
                    /*Establece la estructura del comando (Sentencia + conexión)*/
                    SqlCommand command = new SqlCommand(query, connection); 
                    
                    /*Se ejecuta la sentencia*/
                    command.ExecuteNonQuery(); 
                }
            }
            catch (Exception e) /*Si es que algo sale mal, se retorna un mensaje indicando el error*/
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }

            /*Si todo sale bien, se carga la data a BD exitosamente*/
            return new OkResult();
        }

        [FunctionName("GetUsers")]
        public static async Task<IActionResult> GetUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user")] HttpRequest req, ILogger log)
        {
            List<ListUserModel> taskList = new List<ListUserModel>();
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Select Id, Nombre, Apellido, Correo from Usuario";
                    SqlCommand command = new SqlCommand(query, connection);
                    var reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        ListUserModel user = new ListUserModel()
                        {
                            Id = (int)reader["Id"],
                            Nombre = reader["Nombre"].ToString(),
                            Apellido = reader["Apellido"].ToString(),
                            Correo = reader["Correo"].ToString(),

                        };
                        taskList.Add(user);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }
            if (taskList.Count > 0)
            {
                return new OkObjectResult(taskList);
            }
            else
            {
                return new NotFoundResult();
            }
        }

        [FunctionName("GetUserById")]
        public static IActionResult GetUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id}")] HttpRequest req, ILogger log, int id)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Select Id, Nombre, Apellido, Correo from Usuario Where Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    da.Fill(dt);
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }
            if (dt.Rows.Count == 0)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(dt);
        }

        [FunctionName("DeleteUser")]
        public static IActionResult DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{Id}")] HttpRequest req, ILogger log, int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Delete from Usuario Where Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestResult();
            }
            return new OkResult();
        }

        [FunctionName("UpdateUser")]
        public static async Task<IActionResult> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/{id}")] HttpRequest req, ILogger log, int id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<CreateUserModel>(requestBody);
            try
            {
                using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    connection.Open();
                    var query = @"Update Usuario Set Nombre = @Nombre , Apellido = @Apellido, Correo = @Correo, Contraseña = @Contraseña Where Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Nombre", input.Nombre);
                    command.Parameters.AddWithValue("@Apellido", input.Apellido);
                    command.Parameters.AddWithValue("@Correo", input.Correo);
                    command.Parameters.AddWithValue("@Contraseña", input.Contraseña);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }
            return new OkResult();
        }
    }
}
