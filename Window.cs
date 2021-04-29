using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace DataAdapter
{
    public partial class Window : Form
    {
        private SqlConnection connection = null;
        private SqlDataAdapter adapter = null;
        private SqlCommandBuilder commandBuilder = null;
        private DataSet dataSet = null;
        private DataTable table = null;
        private string connectionString = "";

        public Window()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;
        }

        private void ClickFill(object sender, EventArgs e)
        {
            try
            {
                SqlConnection connection = new SqlConnection(connectionString);
                string commandSelectText = textBox.Text;
                adapter = new SqlDataAdapter(commandSelectText, connection);
                commandBuilder = new SqlCommandBuilder(adapter);
                dataSet = new DataSet();
                adapter.Fill(dataSet, "Products");
                dataGridView.DataSource = dataSet.Tables["Products"];
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void ClickUpdate(object sender, EventArgs e)
        {
            adapter.Update(dataSet, "Products");
        }

        private void ClickAsync(object sender, EventArgs e)
        {
            const string asyncEnable = "Asynchronous Processing=true";

            if (!connectionString.Contains(asyncEnable))
            {
                connectionString = String.Format("{0}; {1}", connectionString, asyncEnable);
            }

            connection = new SqlConnection(connectionString);
            SqlCommand command = connection.CreateCommand();

            command.CommandText = "WAITFOR DELAY '00:00:02'; SELECT * FROM Products;";
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;

            try
            {
                connection.Open();
                AsyncCallback callback = new AsyncCallback(GetDataCallback);
                command.BeginExecuteReader(callback, command);
                MessageBox.Show("Added thread is working...");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void GetDataCallback(IAsyncResult result)
        {
            SqlDataReader reader = null;
            try
            {
                SqlCommand command = (SqlCommand) result.AsyncState;
                reader = command.EndExecuteReader(result);
                table = new DataTable();
                int line = 0;

                do
                {
                    while (reader.Read())
                    {
                        if (line == 0)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                table.Columns.Add(reader.GetName(i));
                            }
                        }

                        line++;
                        DataRow row = table.NewRow();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[i] = reader[i];
                        }

                        table.Rows.Add(row);
                    }

                } while (reader.NextResult());

                DgvAction();
            }
            catch (Exception ex)
            {
                MessageBox.Show("From Callback 1:" + ex.Message);
            }
            finally
            {
                try
                {
                    if (!reader.IsClosed)
                    {
                        reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("From Callback 2:" + ex.Message);
                }
            }
        }

        private void DgvAction()
        {
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke(new Action(DgvAction));
                return;
            }

            dataGridView.DataSource = table;
        }
    }
}
