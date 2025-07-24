using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace farmacia02
{
    public partial class F : Form
    {
        private string connectionString = @"Server=MANLP03-PGS;Database=farmacia;Integrated Security=true;";
        private SqlConnection connection;

        public F()
        {
            InitializeComponent();
            InitializeDatabase();
            LoadData();
        }

        private void InitializeDatabase()
        {
            try
            {
                connection = new SqlConnection(connectionString);
                connection.Open();
                MessageBox.Show("¡Conexión exitosa a la base de datos!", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                connection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadData()
        {
            LoadMedicamentos();
            CheckAlertas();
        }

        private void CheckAlertas()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();


                    string queryStockBajo = @"
                        SELECT COUNT(*) 
                        FROM inventario i
                        JOIN medicamento m ON i.medicamento_id = m.id
                        WHERE i.cantidad <= i.stock_minimo";

                    SqlCommand cmd = new SqlCommand(queryStockBajo, conn);
                    int stockBajo = (int)cmd.ExecuteScalar();

                    if (stockBajo > 0)
                    {
                        MessageBox.Show($"⚠️ {stockBajo} medicamentos con stock bajo",
                            "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando alertas: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadMedicamentos()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT nombre FROM medicamento ORDER BY nombre";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuscarMedicamento(string nombre)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            m.nombre,
                            p.nombre as proveedor,
                            SUM(i.cantidad) as stock,
                            m.precio_venta
                        FROM medicamento m
                        LEFT JOIN proveedor p ON m.proveedor_id = p.id
                        LEFT JOIN inventario i ON m.id = i.medicamento_id
                        WHERE m.nombre LIKE @nombre
                        GROUP BY m.nombre, p.nombre, m.precio_venta";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nombre", "%" + nombre + "%");

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {

                        Proveedor.Text = reader["proveedor"].ToString();
                        Inventario.Text = reader["stock"].ToString();
                    }
                    else
                    {

                        Proveedor.Text = "";
                        Inventario.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Nombredelmedicamento_TextChanged(object sender, EventArgs e)
        {

            if (Nombredelmedicamento.Text.Length > 2)
            {
                BuscarMedicamento(Nombredelmedicamento.Text);
            }
        }

        private void Guardar_Click(object sender, EventArgs e)
        {
            try
            {

                if (string.IsNullOrEmpty(Nombredelmedicamento.Text))
                {
                    MessageBox.Show("Ingrese el nombre del medicamento", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

         
                string medicamento = Nombredelmedicamento.Text;
                string cliente = "Cliente Genérico"; 
                int cantidad = (int)Cantidad.Value;

            a
                RegistrarVenta(cliente, GetMedicamentoId(medicamento), cantidad);

                
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegistrarVenta(string cliente, int medicamentoId, int cantidad)
        {
            SqlTransaction transaction = null;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();


                    string checkStock = "SELECT SUM(cantidad) FROM inventario WHERE medicamento_id = @medicamentoId";
                    SqlCommand cmdCheck = new SqlCommand(checkStock, conn, transaction);
                    cmdCheck.Parameters.AddWithValue("@medicamentoId", medicamentoId);
                    int stockDisponible = (int)(cmdCheck.ExecuteScalar() ?? 0);

                    if (stockDisponible < cantidad)
                    {
                        MessageBox.Show($"Stock insuficiente. Disponible: {stockDisponible}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                    string getPrecio = "SELECT precio_venta FROM medicamento WHERE id = @medicamentoId";
                    SqlCommand cmdPrecio = new SqlCommand(getPrecio, conn, transaction);
                    cmdPrecio.Parameters.AddWithValue("@medicamentoId", medicamentoId);
                    decimal precio = (decimal)cmdPrecio.ExecuteScalar();
                    decimal total = precio * cantidad;


                    string insertVenta = "INSERT INTO venta (cliente, total) OUTPUT INSERTED.id VALUES (@cliente, @total)";
                    SqlCommand cmdVenta = new SqlCommand(insertVenta, conn, transaction);
                    cmdVenta.Parameters.AddWithValue("@cliente", cliente);
                    cmdVenta.Parameters.AddWithValue("@total", total);
                    int ventaId = (int)cmdVenta.ExecuteScalar();

                    string insertDetalle = "INSERT INTO detalle_venta (venta_id, medicamento_id, cantidad, precio) VALUES (@ventaId, @medicamentoId, @cantidad, @precio)";
                    SqlCommand cmdDetalle = new SqlCommand(insertDetalle, conn, transaction);
                    cmdDetalle.Parameters.AddWithValue("@ventaId", ventaId);
                    cmdDetalle.Parameters.AddWithValue("@medicamentoId", medicamentoId);
                    cmdDetalle.Parameters.AddWithValue("@cantidad", cantidad);
                    cmdDetalle.Parameters.AddWithValue("@precio", precio);
                    cmdDetalle.ExecuteNonQuery();

                    string updateInventario = @"
                        UPDATE TOP(1) inventario 
                        SET cantidad = cantidad - @cantidadVendida
                        WHERE medicamento_id = @medicamentoId AND cantidad > 0
                        ORDER BY fecha_vencimiento";

                    SqlCommand cmdUpdate = new SqlCommand(updateInventario, conn, transaction);
                    cmdUpdate.Parameters.AddWithValue("@cantidadVendida", cantidad);
                    cmdUpdate.Parameters.AddWithValue("@medicamentoId", medicamentoId);
                    cmdUpdate.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show($"Venta registrada exitosamente!\nTotal: ${total:F2}", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);


                    LoadData();
                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                MessageBox.Show($"Error registrando venta: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetMedicamentoId(string nombre)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id FROM medicamento WHERE nombre = @nombre";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    object result = cmd.ExecuteScalar();
                    return result != null ? (int)result : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private void LimpiarFormulario()
        {
            Nombredelmedicamento.Text = "";
            Proveedor.Text = "";
            Inventario.Text = "";
            Ventas.Text = "";
            DetalleDeVenta.Text = "";
            Cantidad.Value = 0;
        }


        private void label1_Click(object sender, EventArgs e)
        {
 
        }

        private void label3_Click(object sender, EventArgs e)
        {
 
        }

        private void F_Load(object sender, EventArgs e)
        {

            this.Text = "Sistema de Farmacia FPR";
        }
    }
}