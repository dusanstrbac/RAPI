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
                    logger.Info("Pokušaj otvaranja veze sa bazom podataka");
                    connection.Open();
                    if (connection.State == ConnectionState.Open)
                        logger.Info("Veza sa bazom podataka je uspešno otvorena");
                    else
                        logger.Error("Otvaranje veze sa bazom podataka nije uspelo");

                    using (SqlCommand command = new SqlCommand("select * From Korisnik Where Korisnik=@ime and Lozinka=@lozinka", connection))
                    {
                        command.Parameters.AddWithValue("@ime", ime);
                        command.Parameters.AddWithValue("@lozinka", lozinka);
                        logger.Debug($"Izvršena SQL komanda: select * From Korisnik Where Korisnik={ime} and Lozinka={lozinka}");
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            logger.Info($"SQL komanda izvršena.  pronaðenih redova: {dt.Rows.Count}");
                            odgovor = dt.Rows.Count == 0 ? "NE" : "DA";
                        }
                    }
                    logger.Info("Zatvorena konekcija sa bazom podataka");
                    connection.Close();
                }
                logger.Info($"Post metoda uspešno izvršena vraæen odgovor={odgovor}");
                return odgovor;

            }
            catch (Exception ex)
            {
                logger.Error($"greške u Post: {ex.Message}");
                return "Dogodila se greška";
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
                                        id = Convert.ToUInt64(row["IID"].ToString()),
                                        name = row["Naziv"].ToString(),
                                        regular_price = Convert.ToDecimal(row["Cena"])
                                    };

                                    string stockQuery = "Select Stanje From Stanje Where Artikal = @artikal And Objekat = '001'";
                                    using (SqlCommand stockCommand = new SqlCommand(stockQuery, connection))
                                    {
                                        stockCommand.Parameters.AddWithValue("@artikal", art.id);

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

        [HttpGet("DajPartnera")]

        public IActionResult DajPartnera(string sifra = null, string maticnibroj = null, string pib = null)
        {
            {
                try
                {
                    Partner partner = new Partner();
                    string connStr = _configuration.GetConnectionString("DefaultConnection");

                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        connection.Open();
                        string artQuery = "Select Partner, Naziv, Adresa, MaticniBroj, PIB From Partner Where (@sifra IS NULL OR Partner = @sifra) AND (@maticnibroj IS NULL OR MaticniBroj = @maticnibroj) AND (@pib IS NULL OR PIB = @pib)";

                        using (SqlCommand command = new SqlCommand(artQuery, connection))
                        {
                            command.Parameters.AddWithValue("@sifra", (object)sifra ?? DBNull.Value);
                            command.Parameters.AddWithValue("@maticnibroj", (object)maticnibroj ?? DBNull.Value);
                            command.Parameters.AddWithValue("@pib", (object)pib ?? DBNull.Value);

                            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                if (dt.Rows.Count > 0)
                                {
                                    partner.sifra = dt.Rows[0]["Partner"].ToString();
                                    partner.naziv = dt.Rows[0]["Naziv"].ToString();
                                    partner.adresa = dt.Rows[0]["Adresa"].ToString();
                                    partner.maticnibroj = dt.Rows[0]["MaticniBroj"].ToString();
                                    partner.pib = dt.Rows[0]["PIB"].ToString();
                                }
                                else
                                {
                                    return NotFound("Partner nije pronaðen.");
                                }
                            }
                        }
                        connection.Close();
                    }
                    return Ok(partner);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error in Post: {ex.Message}");
                    return null;
                }
            }

        }

        [HttpPost("DodajPartnera")]
        public IActionResult DodajPartnera(string sifra, string naziv, string adresa, string maticnibroj, string pib)
        {
            try
            {
                string connStr = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO Partner (Partner, Naziv, Adresa, MaticniBroj, PIB) VALUES (@sifra, @naziv, @adresa, @maticnibroj, @pib)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sifra", sifra);
                        command.Parameters.AddWithValue("@naziv", naziv);
                        command.Parameters.AddWithValue("@adresa", adresa);
                        command.Parameters.AddWithValue("@maticnibroj", maticnibroj);
                        command.Parameters.AddWithValue("@pib", pib);

                        int result = command.ExecuteNonQuery();

                        if (result > 0)
                        {
                            return Ok("Partner uspešno dodat.");
                        }
                        else
                        {
                            return BadRequest("Nije moguæe dodati partnera.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in Post: {ex.Message}");
                return StatusCode(500, "Interni server error. Došlo je do greške prilikom obrade zahteva.");
            }
        }
    }
}