using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace RAPI.Controllers
{

    [Route("api")]
    [ApiController]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly log logger;

        public WeatherForecastController(IConfiguration configuration)
        {
            _configuration = configuration;
            logger = new log();
            logger.Info("kontroler API-a instanciran");
        }
        [HttpGet("DajKorisnika", Name = "DajKorisnika")]
        public String Post(string ime, string lozinka)
        {
            String odgovor;
            try
            {

                logger.Info($"DajKorisnika pozvan sa ime: {ime} lozina: {lozinka}");
                string connStr = _configuration.GetConnectionString("DefaultConnection");
                //connStr = "Data Source=PELENOV;Initial Catalog=omegalo;Integrated Security=True;UID=sa;PWD=sasa";
                //String connectionString = "Data Source=LENOPED;Initial Catalog=PH-front;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    logger.Info("Poku�aj otvaranja veze sa bazom podataka");
                    connection.Open();
                    if (connection.State == ConnectionState.Open)
                        logger.Info("Veza sa bazom podataka je uspe�no otvorena");
                    else
                        logger.Error("Otvaranje veze sa bazom podataka nije uspelo");

                    using (SqlCommand command = new SqlCommand("select * From Korisnik Where Korisnik=@ime and Lozinka=@lozinka", connection))
                    {
                        command.Parameters.AddWithValue("@ime", ime);
                        command.Parameters.AddWithValue("@lozinka", lozinka);
                        logger.Debug($"Izvr�ena SQL komanda: select * From Korisnik Where Korisnik={ime} and Lozinka={lozinka}");
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            logger.Info($"SQL komanda izvr�ena.  prona�enih redova: {dt.Rows.Count}");
                            odgovor = dt.Rows.Count == 0 ? "NE" : "DA";
                        }
                    }
                    logger.Info("Zatvorena konekcija sa bazom podataka");
                    connection.Close();
                }
                logger.Info($"Post metoda uspe�no izvr�ena vra�en odgovor={odgovor}");
                return odgovor;

            }
            catch (Exception ex)
            {
                logger.Error($"gre�ke u Post: {ex.Message}");
                return "Dogodila se gre�ka";
            }
        }

        [HttpGet("Art", Name = "DajArtikle")]
        public List<Artikli> DajArt()
        {
            try
            {
                List<Artikli> artikli = new List<Artikli>();
                string connStr = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    string artQuery = "Select Artikal as IID, Naziv, PTCena as Cena From vStavkaCTC Where Artikal <> '' And CenovnikTC in (Select CenovnikTC from CenovnikTC Where opis='WEB')";

                    using (SqlCommand command = new SqlCommand(artQuery, connection))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    Artikli art = new Artikli
                                    {
                                        IID = row["IID"].ToString(),
                                        Naziv = row["Naziv"].ToString(),
                                        Cena = row["Cena"].ToString()
                                    };

                                    string stockQuery = "Select Stanje From Stanje Where Artikal = @artikal And Objekat = '001'";
                                    using (SqlCommand stockCommand = new SqlCommand(stockQuery, connection))
                                    {
                                        stockCommand.Parameters.AddWithValue("@artikal", art.IID);

                                        using (SqlDataAdapter stockAdapter = new SqlDataAdapter(stockCommand))
                                        {
                                            DataTable stockDt = new DataTable();
                                            stockAdapter.Fill(stockDt);

                                            art.Kolicina = stockDt.Rows.Count > 0 ? stockDt.Rows[0]["Stanje"].ToString() : "0";
                                        }
                                    }

                                    artikli.Add(art);
                                }
                            }
                        }
                    }
                    connection.Close();
                }
                return artikli;
            }
            catch (Exception ex)
            {
                logger.Error($"Error in Post: {ex.Message}");
                return null;
            }

        }
    }
}