using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
        public String DajKorisnika(string ime, string lozinka)
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
        public List<Artikli> DajArt(string partner)
        {
            string klasa = "";
            string tip = "";
            string[] ar;
            try
            {
                List<Artikli> artikli = new List<Artikli>();
                string connStr = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    string artQuery = "Select dbo.zrdesc(Var,'ExtPK') From Partner Where Partner='" + partner + "'";
                    using (SqlCommand command = new SqlCommand(artQuery, connection))
                    {
                        klasa = command.ExecuteScalar().ToString();
                    }

                    if (klasa == "")
                    {
                        connection.Close();
                        return null;
                    }
                    ar = klasa.Split(",");
                    tip = ar[0];
                    klasa = ar[1];
                    if (tip == "" | klasa == "")
                        return null;
                    artQuery = " Select Artikal.Artikal as IID,Artikal.Naziv as Naziv, 0 as Cena From Artikal INNER JOIN StavkaAK ON Artikal.Artikal = StavkaAK.Artikal Where Artikal.Artikal IN(Select Artikal From StavkaAK Where TipKlas = '" + tip + "' And Klas = '" + klasa + "') And StavkaAK.TipKlas = '" + tip + "' And StavkaAK.Klas = '" + klasa + "' Order By Artikal.Artikal";
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

                                    string stockQuery = "Select Stanje From Stanje Where Artikal ='" + art.id + "' And Objekat = '001'";
                                    using (SqlCommand stockCommand = new SqlCommand(stockQuery, connection))
                                    {
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
                        string sWhere = "";
                        if (sifra != null)
                            sWhere = "Partner='" + sifra + "'";
                        else
                            if (pib != null)
                            sWhere = "PIB='" + pib + "'";
                        else
                                if (maticnibroj != null)
                            sWhere = "MaticniBroj='" + maticnibroj + "'";

                        if (sWhere == "")
                            return NotFound("Partner nije pronaðen.");

                        connection.Open();
                        string artQuery = "Select Partner, Naziv, Adresa, MaticniBroj, PIB From Partner Where " + sWhere;

                        using (SqlCommand command = new SqlCommand(artQuery, connection))
                        {
                            //command.Parameters.AddWithValue("@sifra", (object)sifra ?? DBNull.Value);
                            //command.Parameters.AddWithValue("@maticnibroj", (object)maticnibroj ?? DBNull.Value);
                            //command.Parameters.AddWithValue("@pib", (object)pib ?? DBNull.Value);

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
                                //else
                                //{
                                //    connection.Close();
                                //    return Ok("Klijent ne postoji");
                                //}
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

        [HttpPost("DodajDokument")]
        public Int64 DodajDokument(string tip, string partner)
        {
            Int64 IIDN = 0;
            Int64 IIDD = 0;
            Int64 result = 0;
            int i = 0;
            string myTip = "15";
            string myObjekat = "NI01";
            string myVrsta = "02";
            SqlCommand comm;
            try
            {
                string connStr = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    string insertQuery = "Select IID from Nalog Where Vrsta='002' and status='' and Datum='" + DateTime.Now.ToString("yyyy-MM-dd") + "'";

                        comm=new SqlCommand(insertQuery,connection);
                        var odgovor = comm.ExecuteScalar();
                        if (odgovor == null)
                        {
                            insertQuery = "Select Min(IID) from Nalog";
                            comm = new SqlCommand(insertQuery, connection);
                            result = Convert.ToInt64(comm.ExecuteScalar().ToString());
                            if (result > 0)
                                result = 0;

                            IIDN = result;
                            for (i = 1; i < 10; i++)
                            {
                                IIDN = IIDN - i;
                                insertQuery = "Insert into Nalog(IID,Vlasnik,Ord,Korak,Vrsta,Datum,Opis) ";
                                insertQuery = insertQuery + "Values(" + IIDN + ",'01',1024,1024,'002','" + DateTime.Now.ToString("yyyy-MM-dd") + "','WEB')";
                                comm = new SqlCommand(insertQuery, connection);
                                try
                                {
                                    comm.ExecuteNonQuery();
                                    result = IIDN;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    if (i == 10)
                                        return 0;
                                }
                            }


                        }
                        else
                            result = Convert.ToInt64(odgovor.ToString());

                        IIDN = result;
                        //dodaj dokument i vrati iiddokumenta
                        insertQuery = "Select Min(IID) from Dokument";
                        comm = new SqlCommand(insertQuery, connection);
                        result = Convert.ToInt64(comm.ExecuteScalar().ToString());
                        IIDD = result;
                        for (i = 1; i < 10; i++)
                        {
                            IIDD = IIDD - i;
                            insertQuery = "Insert into Dokument(IID,IIDNaloga,Ord,Korak,Vrsta,Datum,Opis,Tip,Partner,Objekat) ";
                            insertQuery = insertQuery + "Values(" + IIDD + "," + IIDN + ",1024,1024,'" + myVrsta + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','WEB','" + myTip + "','" + partner + "','" + myObjekat + "')";
                            comm = new SqlCommand(insertQuery, connection);
                            try
                            {
                                comm.ExecuteNonQuery();
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (i == 10)
                                    return 0;
                            }
                        }

                        return IIDD;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in Post: {ex.Message}");
                return 0;
            }
        }
        [HttpPost("DodajStavku")]
        public Int64 DodajSavku(ulong iidD, ulong artikal, double kolicina)
        {
            int i = 0;
            Int64 j = 0;
            SqlCommand comm;
            try
            {
                string connStr = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    string insertQuery = "Select  count(*) from RMStavka Where iiddokumenta=" + iidD;

                    comm = new SqlCommand(insertQuery, connection);
                    j = Convert.ToInt64(comm.ExecuteScalar().ToString());


                    insertQuery = "Select Min(IID) from RMStavka";
                    comm = new SqlCommand(insertQuery, connection);
                    Int64 result = Convert.ToInt64(comm.ExecuteScalar().ToString());
                    if (result > 0)
                            result = 0;

                        for (i = 1; i < 10; i++)
                        {
                            result = result - 1;
                            insertQuery = "Insert into RMStavka(IID,IIDDokumenta,Ord,Korak,Artikal,Kolicina) ";
                            insertQuery = insertQuery + "Values(" + result + "," + iidD + ",1024," + j * 1024 + ",'" + artikal + "'," + kolicina + ")";
                            comm = new SqlCommand(insertQuery, connection);
                            try
                            {
                                comm.ExecuteNonQuery();
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (i == 10)
                                    return 0;
                            }
                        }


                        return result;
                    
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in Post: {ex.Message}");
                return 0;
            }
        }

    }
}