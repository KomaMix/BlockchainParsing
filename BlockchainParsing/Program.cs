using Newtonsoft.Json.Linq;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;


string blockHash = "00000000000000000001ef5340d312f07870d2ce14fecc2dade7a483c59c16b7";
string url = $"https://blockchain.info/rawblock/{blockHash}";

// Создание клиента HTTP
HttpClient client = new HttpClient();

try
{
	// Отправка запроса и парсинг данных блока
	JObject blockJson = await FetchBlockData(client, url);
	Block block = blockJson.ToObject<Block>();

	// Запись данных в CSV файлы
	CsvHelperExtensions.WriteBlocksCsv(block);
	CsvHelperExtensions.WriteTransactionsCsv(block.Tx);

	// Извлечение всех inputs в общий список
	var inputs = block.Tx.SelectMany(tx => tx.Inputs).ToList();
	CsvHelperExtensions.WriteInputsCsv(inputs);

	// Извлечение всех outputs в общий список
	var outputs = block.Tx.SelectMany(tx => tx.Out).ToList();
	CsvHelperExtensions.WriteOutputsCsv(outputs);





	// Проверка гипотез (тест данных) о количестве данных
	//Console.WriteLine(Test.MaxOutputSpending(inputs, outputs)); //output_spending всегда меньше 2
	//Console.WriteLine(Test.NoEqualTxIndex(block.Tx)); //проверка того, что input связано с определенной транзакцией

	Console.WriteLine("Данные успешно сохранены в CSV файлы.");
}
catch (Exception ex)
{
	Console.WriteLine($"Произошла ошибка: {ex.Message}");
}


// Модели данных для извлечения из файла json
static async Task<JObject> FetchBlockData(HttpClient client, string url)
{
	var response = await client.GetStringAsync(url);
	return JObject.Parse(response);
}

public class Block
{
	public string Hash { get; set; }
	public int Ver { get; set; }
	public string Prev_Block { get; set; }
	public string Mrkl_Root { get; set; }
	public long Time { get; set; }
	public int Bits { get; set; }
	public List<string> Next_Block { get; set; }
	public long Fee { get; set; }
	public long Nonce { get; set; }
	public int N_Tx { get; set; }
	public int Size { get; set; }
	public int Block_Index { get; set; }
	public bool Main_Chain { get; set; }
	public int Height { get; set; }
	public int Weight { get; set; }
	public List<Transaction> Tx { get; set; }
}

public class Transaction
{
	public string Hash { get; set; }
	public int Ver { get; set; }
	public int Vin_Sz { get; set; }
	public int Vout_Sz { get; set; }
	public int Size { get; set; }
	public int Weight { get; set; }
	public long Fee { get; set; }
	public string Relayed_By { get; set; }
	public long Lock_Time { get; set; }
	public long Tx_Index { get; set; }
	public bool Double_Spend { get; set; }
	public long Time { get; set; }
	public int Block_Index { get; set; }
	public int Block_Height { get; set; }
	public List<Input> Inputs { get; set; }
	public List<Output> Out { get; set; }
}

public class Input
{
	public long Sequence { get; set; }
	public string Witness { get; set; }
	public string Script { get; set; }
	public long Index { get; set; }
	public PrevOut Prev_Out { get; set; }

	public string Prev_Out_Spending_Outpoints_Tx_Index
	{
		get => Prev_Out?.Spending_Outpoints != null ?
			   string.Join(";", Prev_Out.Spending_Outpoints.Select(sp => sp.Tx_Index.ToString())) : string.Empty;
	}

	public string Prev_Out_Spending_Outpoints_N
	{
		get => Prev_Out?.Spending_Outpoints != null ?
			   string.Join(";", Prev_Out.Spending_Outpoints.Select(sp => sp.N.ToString())) : string.Empty;
	}
}

public class PrevOut
{
	public int Type { get; set; }
	public bool Spent { get; set; }
	public long Value { get; set; }
	public List<SpendingOutpoint> Spending_Outpoints { get; set; }
	public long N { get; set; }
	public long Tx_Index { get; set; }
	public string Script { get; set; }
	public string Addr { get; set; }
}

public class SpendingOutpoint
{
	public long Tx_Index { get; set; }
	public int N { get; set; }
}

public class Output
{
	public int Type { get; set; }
	public bool Spent { get; set; }
	public long Value { get; set; }
	public List<SpendingOutpoint> Spending_Outpoints { get; set; }
	public long N { get; set; }
	public long Tx_Index { get; set; }
	public string Script { get; set; }
	public string Addr { get; set; }

	public string Spending_Outpoints_Tx_Index
	{
		get => Spending_Outpoints != null ?
			   string.Join(";", Spending_Outpoints.Select(sp => sp.Tx_Index.ToString())) : string.Empty;
	}

	public string Spending_Outpoints_N
	{
		get => Spending_Outpoints != null ?
			   string.Join(";", Spending_Outpoints.Select(sp => sp.N.ToString())) : string.Empty;
	}
}


// Тест некоторых аспектов данных
public class Test
{
	// Определяем максимальный размер массива output_spending у inputs и outputs
	public static int MaxOutputSpending(List<Input> inputs, List<Output> outputs)
	{
		int result = 0;

		foreach (var input in inputs)
		{
			result = Math.Max(result, input.Prev_Out.Spending_Outpoints.Count);
		}

		foreach (var output in outputs)
		{
			result = Math.Max(result, output.Spending_Outpoints.Count);
		}

		return result;
	}

	// Проверка гипотезы о том, что input связано с transaction с помощью po_so_tx_index (в файле csv это название)
	public static bool NoEqualTxIndex(List<Transaction> transactions)
	{
		foreach (var tx in transactions)
		{
			foreach (var input in tx.Inputs)
			{
				if (input.Prev_Out_Spending_Outpoints_Tx_Index != tx.Tx_Index.ToString())
				{
					return false;
				}
			}
		}
		return true;
	}
}


public class CsvHelperExtensions
{
	// Записывает информацию о блоке
	public static void WriteBlocksCsv(Block block, string filePath = "block.csv")
	{
		using (var writer = new StreamWriter(filePath))
		using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
		{
			csv.Context.RegisterClassMap<BlockMap>();
			csv.WriteRecords(new List<Block> { block });
		}
	}

	// Записывает данные о транзакциях
	public static void WriteTransactionsCsv(List<Transaction> transactions, string filePath = "transactions.csv")
	{
		using (var writer = new StreamWriter(filePath))
		using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
		{
			csv.Context.RegisterClassMap<TransactionMap>();
			csv.WriteRecords(transactions);
		}
	}

	// Записывает даннные о inputs
	public static void WriteInputsCsv(List<Input> inputs, string filePath = "inputs.csv")
	{
		using (var writer = new StreamWriter(filePath))
		using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
		{
			csv.Context.RegisterClassMap<InputMap>();
			csv.WriteRecords(inputs);
		}
	}

	// Записывает данные о outputs
	public static void WriteOutputsCsv(List<Output> outputs, string filePath = "outputs.csv")
	{
		using (var writer = new StreamWriter(filePath))
		using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
		{
			csv.Context.RegisterClassMap<OutputMap>();
			csv.WriteRecords(outputs);
		}
	}
}


// Далее идут классы сопоставления, которые используются в CsvHelperExtensions
// Они управляют загаловками и столбцами, которые будут записаны в csv файлы
// Благодаря этому можно хорошо настроить итоговый вид csv таблиц
// Так же можно управлять порядком записи, вызвав Index у каждой точки
// Например: Map(m => m.Hash).Name("hash").Index(2);
public class BlockMap : ClassMap<Block>
{
	public BlockMap()
	{
		Map(m => m.Hash).Name("hash");
		Map(m => m.Ver).Name("ver");
		Map(m => m.Prev_Block).Name("prev_block");
		Map(m => m.Mrkl_Root).Name("mrkl_root");
		Map(m => m.Time).Name("time");
		Map(m => m.Bits).Name("bits");
		Map(m => m.Next_Block).Name("next_block");
		Map(m => m.Fee).Name("fee");
		Map(m => m.Nonce).Name("nonce");
		Map(m => m.N_Tx).Name("n_tx");
		Map(m => m.Size).Name("size");
		Map(m => m.Block_Index).Name("block_index");
		Map(m => m.Main_Chain).Name("main_chain");
		Map(m => m.Height).Name("height");
		Map(m => m.Weight).Name("weight");
		Map(m => m.Tx).Ignore(); // Игнорируем список транзакций
	}
}

public class TransactionMap : ClassMap<Transaction>
{
	public TransactionMap()
	{
		Map(m => m.Hash).Name("hash");
		Map(m => m.Ver).Name("ver");
		Map(m => m.Vin_Sz).Name("vin_sz");
		Map(m => m.Vout_Sz).Name("vout_sz");
		Map(m => m.Size).Name("size");
		Map(m => m.Weight).Name("weight");
		Map(m => m.Fee).Name("fee");
		Map(m => m.Relayed_By).Name("relayed_by");
		Map(m => m.Lock_Time).Name("lock_time");
		Map(m => m.Tx_Index).Name("tx_index");
		Map(m => m.Double_Spend).Name("double_spend");
		Map(m => m.Time).Name("time");
		Map(m => m.Block_Index).Name("block_index");
		Map(m => m.Block_Height).Name("block_height");
		Map(m => m.Inputs).Ignore(); // Игнорируем список входов
		Map(m => m.Out).Ignore(); // Игнорируем список выходов
	}
}

public class InputMap : ClassMap<Input>
{
	public InputMap()
	{
		Map(m => m.Sequence).Name("sequence");
		Map(m => m.Witness).Name("witness");
		Map(m => m.Script).Name("script");
		Map(m => m.Index).Name("index");

		Map(m => m.Prev_Out.Type).Name("po_type");
		Map(m => m.Prev_Out.Spent).Name("po_spent");
		Map(m => m.Prev_Out.Value).Name("po_value");
		Map(m => m.Prev_Out_Spending_Outpoints_Tx_Index).Name("po_so_tx_index");
		Map(m => m.Prev_Out_Spending_Outpoints_N).Name("po_so_n");
		Map(m => m.Prev_Out.N).Name("po_n");
		Map(m => m.Prev_Out.Tx_Index).Name("po_tx_index");
		Map(m => m.Prev_Out.Script).Name("po_script");
		Map(m => m.Prev_Out.Addr).Name("po_addr");
	}
}

public class OutputMap : ClassMap<Output>
{
	public OutputMap()
	{
		Map(m => m.Type).Name("type");
		Map(m => m.Spent).Name("spent");
		Map(m => m.Value).Name("value");
		Map(m => m.Spending_Outpoints_Tx_Index).Name("so_tx_index");
		Map(m => m.Spending_Outpoints_N).Name("so_n");
		Map(m => m.N).Name("n");
		Map(m => m.Tx_Index).Name("tx_index");
		Map(m => m.Script).Name("script");
		Map(m => m.Addr).Name("addr");
	}
}