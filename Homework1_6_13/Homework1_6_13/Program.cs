using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Homework1_6_13
{
    class Program
    {
        static void Main(string[] args)
        {
            CarService carService = new CarService();
            carService.Work();
        }
    }

    abstract class Trader
    {
        protected float Money;

        public Trader(float money)
        {
            Money = money;
        }
    }

    class Customer: Trader
    {
        private Receipt _receipt;

        public Customer (float money): base(money) 
        {
            BrokenSparePartName = CarParts.GetRandomName();
        }

        public string BrokenSparePartName { get; private set; }

        public void TakeFinePrice(CarService carService)
        {
            Money += carService.FinePrice;
        }

        public void TakeErrorFinePrice(CarService carService)
        {
            Money += carService.SparePartPrice;
        }

        public void AddReceipt(Receipt receipt)
        {
            _receipt = receipt;
        }

        public void PayToWork()
        {
            Money -= _receipt.PriceForWork + _receipt.PriceForSparePart;

            if (Money < 0)
            {
                Console.WriteLine("У клиента не хватило денег, ему пришлось взять в долг и он попал в рабство");
                Money = 0;
            }
            else
            {
                Console.WriteLine("Успешно оплачено");
            }
        }
    }

    class CarService : Trader
    {
        private const float DefaultStartMoney = 2000;
        private const float PriceForWork = 600;

        private Stock _stock;
        private List<Receipt> _receipts;
        private Queue<Customer> _customers;
        private bool _isWork;

        public CarService(float money = DefaultStartMoney) : base(money)
        {
            _stock = new Stock(CarParts.CreateCellsSpareParts());
            _receipts = new List<Receipt>();
            _customers = new Queue<Customer>();
            FinePrice = 500;
            SparePartPrice = 0;

            FillCustomers();
        }

        public float FinePrice { get; private set; }
        public float SparePartPrice { get; private set; }

        public void Work()
        {
            const string AccommodateCustomerCommand = "1";
            const string ExitCommand = "3";
            const string ShowReceiptsCommnad = "2";

            _isWork = true;

            while (_isWork)
            {
                Console.Clear();
                Console.WriteLine($"Деньги: {Money}\n{AccommodateCustomerCommand}. Принять клиента");
                Console.WriteLine($"{ShowReceiptsCommnad}. Вывести чеки");
                Console.WriteLine($"{ExitCommand}. Закрыть автосервис");

                switch (Console.ReadLine())
                {
                    case AccommodateCustomerCommand:
                        AccommodateCustomer();
                        break;

                    case ShowReceiptsCommnad:
                        ShowReceipts();
                        break;

                    case ExitCommand:
                        _isWork = false;
                        break;

                    default:
                        Console.WriteLine("Введена неверная команда");
                        break;
                }

                Console.ReadKey();
            }
        }

        private void ShowReceipts()
        {
            if (_receipts.Count == 0)
            {
                Console.WriteLine("Пусто");
            }

            for (int i = 0; i < _receipts.Count; i++)
            {
                Console.Write($"{i+1}. ");
                _receipts[i].ShowInfo();
            }
        }

        private void AccommodateCustomer()
        {
            const string UseDetailCommand = "1";
            const string PayFineCommand = "2";

            if (_customers.Count > 0)
            {
                Customer customer = _customers.Dequeue();
                bool isWorkOnOrder = true;

                while (isWorkOnOrder)
                {
                    Console.Clear();
                    Console.WriteLine($"Вам необходимо заменить {customer.BrokenSparePartName}. Что будете делать?");
                    Console.WriteLine($"\n{UseDetailCommand}. Пойти на склад за деталью");
                    Console.WriteLine($"{PayFineCommand}. Отказать клиенту");

                    switch (Console.ReadLine())
                    {
                        case UseDetailCommand:
                            UseDetail(customer);
                            isWorkOnOrder = false;
                            break;

                        case PayFineCommand:
                            PayFine(customer);
                            isWorkOnOrder = false;
                            break;

                        default:
                            Console.WriteLine("Введена неверная команда");
                            break;
                    }

                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("Клиенты кончились");
            }
        }

        private void UseDetail(Customer customer)
        {
            if (_stock.HaveSpareParts())
            {
                bool isSuccessInput = false;

                while (isSuccessInput == false)
                {
                    Console.Clear();
                    _stock.ShowInfo();
                    Console.WriteLine("\nВыберете номер детали, которую хотите использовать для ремонта");

                    if (int.TryParse(Console.ReadLine(), out int userInput))
                    {
                        userInput--;

                        if (_stock.TryUseSparePart(userInput, out string userSparePartName))
                        {
                            isSuccessInput = true;

                            if (customer.BrokenSparePartName == userSparePartName)
                            {
                                SetSparePartPrice(customer);
                                Receipt receipt = new Receipt(PriceForWork, SparePartPrice);
                                _receipts.Add(receipt);
                                customer.AddReceipt(receipt);
                                Money += PriceForWork + SparePartPrice;
                                customer.PayToWork();
                            }
                            else
                            {
                                PayErrorFine(customer);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Неверный формат ввода");
                    }

                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("Запчастей не осталось, рекомендуем закрыться до новой поставки");
                PayFine(customer);
            }
        }

        private void PayErrorFine(Customer customer)
        {
            SetSparePartPrice(customer);
            customer.TakeErrorFinePrice(this);
            Money -= SparePartPrice;

            _isWork = !IsBankruptcy();
            SparePartPrice = 0;
        }

        private void SetSparePartPrice(Customer customer )
        {
            float price;

            if (_stock.TryGetSparePartPrice(customer.BrokenSparePartName, out price) == false)
            {
                Console.WriteLine("Ошибка имени поломанной детали");
            }

            SparePartPrice = price;
        }

        private void PayFine(Customer customer)
        {
            customer.TakeFinePrice(this);
            Money -= FinePrice;

            _isWork = !IsBankruptcy();
        }

        private bool IsBankruptcy()
        {
            if (Money < 0)
            {
                Console.WriteLine("Вы влезли в долги и стали банкротом. Автосервис более не Ваш");
                Money = 0;
                return true;
            }

            Console.WriteLine("Вы заплатили штраф, клиент ушел не такой недовольный");
            return false;
        }

        private void FillCustomers()
        {
            int countCustomers = UserUtilits.GenerateRandomNumber(5, 20);
            int minMoney = 500;
            int maxMoney = 25000;

            for (int i = 0; i < countCustomers; i++)
            {
                _customers.Enqueue(new Customer(UserUtilits.GenerateRandomNumber(minMoney, maxMoney)));
            }
        }
    }

    class Receipt
    {
        public Receipt (float priceForWork, float priceForSparePart)
        {
            PriceForWork = priceForWork;
            PriceForSparePart = priceForSparePart;
        }

        public float PriceForWork { get; private set; }
        public float PriceForSparePart { get; private set; }

        public void ShowInfo()
        {
            Console.WriteLine($"Ремонт детали - {PriceForSparePart}, плата за работу - {PriceForWork}. В сумме оплачено - {PriceForSparePart + PriceForWork}");
        }
    }

    class Stock
    {
        private List<CellSparePart> _spareParts;

        public Stock (List<CellSparePart> spareParts)
        {
            bool isErrorName = false;

            for (int i=0; i < spareParts.Count && isErrorName == false; i++)
            {
                if (CarParts.HaveName(spareParts[i].SparePart.Name) == false)
                {
                    isErrorName = true;
                }
            }

            if (isErrorName == false)
            {
                _spareParts = spareParts;
            }
            else
            {
                Console.WriteLine("Ошибка, ввод несуществующей детали");
                _spareParts = null;
            }
        }

        public void ShowInfo()
        {
            for (int i = 0; i < _spareParts.Count; i++)
            {
                Console.Write($"{i+1}. ");
                _spareParts[i].ShowInfo();
                Console.WriteLine();
            }
        }

        public bool TryGetSparePartPrice(string sparePartName, out float price)
        {
            foreach (var sparePart in _spareParts)
            {
                if (sparePart.SparePart.Name == sparePartName)
                {
                    price = (float)sparePart.SparePart.Price;
                    return true;
                }
            }

            price = 0;
            return false;
        }

        public bool TryUseSparePart(int index, out string usedSparePartName)
        {
            if (index >= 0 && index < _spareParts.Count)
            {
                if (_spareParts[index].TryUse(out usedSparePartName))
                {
                    return true;
                }
            }

            usedSparePartName = "";
            Console.WriteLine("Запчасти с таким номером нет");
            return false;
        }

        public bool HaveSpareParts()
        {
            return _spareParts.Count > 0;
        }
    }

    class SparePart
    {
        public SparePart(string name, float? price = null)
        {
            Name = name;
            Price = price;
        }

        public string Name { get; private set; }
        public float? Price { get; private set; }

        public void ShowInfo()
        {
            Console.Write(Name);

            if (Price != null)
            {
                Console.Write($", цена за шт - {Price}");
            }
        }
    }

    class CellSparePart
    {
        public CellSparePart(SparePart sparePart, int quantity)
        {
            SparePart = sparePart;
            Quantity = quantity;
        }

        public SparePart SparePart { get; private set; }
        public int Quantity { get; private set; }

        public bool TryUse (out string usedSparePartName)
        {
            if (HaveQuantity())
            {
                Quantity--;
                usedSparePartName = SparePart.Name;
                return true;
            }

            usedSparePartName = "";
            Console.WriteLine("Не хватает запчастей");
            return false;
        }

        public void ShowInfo()
        {
            SparePart.ShowInfo();
            Console.Write($" Остаток: {Quantity} шт");
        }

        private bool HaveQuantity()
        {
            return Quantity > 0;
        }
    }

    class CarParts
    {
        static CarParts()
        {
            Names.Add("Блок управления ABS");
            Names.Add("Тормозной суппорт");
            Names.Add("Тормозные диски");
            Names.Add("Глушитель");
            Names.Add("Колесо");
            Names.Add("Аккумулятор");
            Names.Add("Сцепление");
            Names.Add("Маховик");
        }

        public static List<string> Names { get; private set; } = new List<string>();

        public static void AddName(string name)
        {
            if (HaveName(name) == false)
            {
                Names.Add(name);
            }
            else
            {
                Console.WriteLine("Запчасть с таким именем уже существует");
            }
        }

        public static bool HaveName(string nameToCheck)
        {
            foreach (var name in Names)
            {
                if (name == nameToCheck)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetRandomName()
        {
            return Names[UserUtilits.GenerateRandomNumber(0, Names.Count - 1)];
        }

        public static List<CellSparePart> CreateCellsSpareParts()
        {
            List<CellSparePart> spareParts = new List<CellSparePart>();
            string errorMessageInputPrice = "Ошибка ввода стоимости, попробуйте заного";
            string errorMessageInputQuantity = "Ошибка ввода количества, попробуйте заного";

            Console.WriteLine("Введите стоимость, за которую хотите продавать запчасть и его количество в наличии:");

            for (int i = 0; i < Names.Count; i++)
            {
                Console.WriteLine($"\n{i+1}. {Names[i]}:");
                Console.Write("Стоимость - ");
                int price = UserUtilits.GetInputWithErrorMessage(errorMessageInputPrice);
                Console.Write("Количество - ");
                int quantity = UserUtilits.GetInputWithErrorMessage(errorMessageInputQuantity);

                spareParts.Add(new CellSparePart(new SparePart(Names[i], price), quantity));
            }

            return spareParts;
        }
    }

    class UserUtilits
    {
        private static Random _random = new Random();

        public static int GenerateRandomNumber(int min, int max)
        {
            return _random.Next(min, max + 1);
        }

        public static int GenerateRandomNumber(int max)
        {
            return _random.Next(0, max + 1);
        }

        public static int GetInputWithErrorMessage(string errorMessage)
        {
            int leftPosition = Console.CursorLeft;
            int topPosition = Console.CursorTop;
            int input;

            while (int.TryParse(Console.ReadLine(), out input) == false)
            {
                Console.Write($"\n{errorMessage}");
                Console.ReadKey();
                Console.SetCursorPosition(0, Console.CursorTop + 1);

                ClearConsoleAfterPosition(leftPosition, topPosition);
            }

            return input;
        }

        private static void ClearConsoleAfterPosition(int leftPosition, int topPosition)
        {
            for (int j = Console.CursorTop; j > topPosition; j--)
            {
                for (int i = Console.CursorLeft; i >= 0; i--)
                {
                    Console.SetCursorPosition(i, j);
                    Console.Write(" ");
                }

                Console.CursorLeft = Console.WindowWidth;
            }

            for (int i = Console.CursorLeft; i >= leftPosition; i--)
            {
                Console.SetCursorPosition(i, topPosition);
                Console.Write(" ");
            }

            Console.SetCursorPosition(leftPosition, topPosition);
        }
    }
}