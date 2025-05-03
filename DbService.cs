using BLSShared.Config;
using BLSShared.Models;
using Microsoft.Extensions.Options;
using NLog;
using Npgsql;
using NpgsqlTypes;
using System.Text;

namespace BLSWebAPI.Services;

public class DbService
{
    #region Properties

    private readonly PostgresConfiguration _config;
    private readonly string _connectionString;
    private Logger _logger { get; } = LogManager.GetCurrentClassLogger();

    #endregion

    #region SQL

    private const string CreateTablesSQL = @"
CREATE TABLE IF NOT EXISTS Passengers (
    row_id SERIAL PRIMARY KEY,
    user_id UUID,
    service_type VARCHAR(128),
    name_surname VARCHAR(256),
    airline VARCHAR(128),
    status VARCHAR(32),
    prolongation INT DEFAULT 0,
    from_time TIMESTAMPTZ DEFAULT NOW(),
    to_time TIMESTAMPTZ DEFAULT NOW(),
    flight_date TIMESTAMPTZ DEFAULT NOW(),
    card_number VARCHAR(64),
    ticket_number VARCHAR(64),
    guests_quantity INTEGER,
    pax_id INTEGER,
    notes VARCHAR(512),
    guest_age INTEGER,
    transaction_number VARCHAR(64),
    seat_number VARCHAR(64),
    voucher_number VARCHAR(64),
    child_age INTEGER,
    declined_code VARCHAR(64),
    amount DECIMAL,
    CB_child BOOL,
    passport_number VARCHAR(64),
    receipt_number VARCHAR(64),
    pass_ref VARCHAR(64),
    is_notifyed BOOL DEFAULT false,
    is_deleted BOOL DEFAULT false,
    deleted_at TIMESTAMPTZ
    );

CREATE TABLE IF NOT EXISTS Settings (
    employee_id BIGINT UNIQUE,  
    grid_settings_json TEXT,
    CONSTRAINT fk_settings
		FOREIGN KEY (employee_id) 
			REFERENCES Employee(id)
);

CREATE TABLE IF NOT EXISTS Employee (
	id SERIAL PRIMARY KEY,
	user_name TEXT UNIQUE,
	password VARCHAR(128)
);

CREATE TABLE IF NOT EXISTS Roles (
	employee_id BIGINT,
	role_name VARCHAR(128),
    UNIQUE(employee_id, role_name)
);
";

    private const string CreateIndexSQL = @"
CREATE UNIQUE INDEX IF NOT EXISTS idx_passengers_rowid ON Passengers(row_id);
CREATE INDEX IF NOT EXISTS idx_passengers_userid ON Passengers(user_id);
CREATE INDEX IF NOT EXISTS idx_passengers_userid_servicetype ON Passengers(user_id, service_type);
CREATE INDEX IF NOT EXISTS idx_passengers_namesurname ON Passengers(name_surname);
CREATE INDEX IF NOT EXISTS idx_passengers_airline ON Passengers(airline);
CREATE INDEX IF NOT EXISTS idx_passengers_status ON Passengers(status);
CREATE INDEX IF NOT EXISTS idx_passengers_fromtime_totime ON Passengers(from_time, to_time);
CREATE INDEX IF NOT EXISTS idx_passengers_cardnumber ON Passengers(card_number);
CREATE INDEX IF NOT EXISTS idx_passengers_ticketnumber ON Passengers(ticket_number);
CREATE INDEX IF NOT EXISTS idx_passengers_isnotifyed ON Passengers(is_notifyed);
CREATE INDEX IF NOT EXISTS idx_passengers_isdeleted ON Passengers(is_deleted);
";

    private const string InsertPassengerSQL = @"
INSERT INTO passengers (
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    prolongation,
    status,
    from_time,
    to_time,
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age,
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code,
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref)

VALUES (
    @user_id,
    @service_type,
    @name_surname,
    @airline,
    @flight_date,
    @prolongation,
    @status,
    @from_time,
    @to_time, 
    @card_number, 
    @ticket_number,
    @guests_quantity, 
    @pax_id, @notes,
    @guest_age,
    @transaction_number,
    @seat_number,
    @voucher_number,
    @child_age, 
    @declined_code,
    @amount, 
    @CB_child,
    @passport_number, 
    @receipt_number,
    @pass_ref)
;
";

    private const string InsertSettingsSQL = @"
INSERT INTO Settings (
    employee_id,  
    grid_settings_json
)
VALUES(
	@employee_id,  
    @grid_settings_json)
ON CONFLICT(employee_id)
DO UPDATE SET
	grid_settings_json=@grid_settings_json
;
";

    private const string InsertOrUpdateEmployeeWithRolesSQL = @"
WITH employee_id AS (
   INSERT INTO Employee (user_name, password)
   VALUES (@user_name, @password)
ON CONFLICT (user_name)
DO UPDATE SET password = EXCLUDED.password
   RETURNING id
)

INSERT INTO Roles (
    employee_id, 
    role_name
)
SELECT employee_id.id, UNNEST(@roles) 
   FROM employee_id
ON CONFLICT (employee_id, role_name)
DO UPDATE SET 
    role_name = EXCLUDED.role_name;
        ";

    private const string DeleteEmployeeSQL = @"
DELETE FROM Roles
WHERE employee_id = (SELECT id FROM Employee WHERE user_name=@user_name);

DELETE FROM Employee
WHERE user_name=@user_name;

;";

    private const string SelectSettingsSQL = @"
SELECT
	employee_id,
    grid_settings_json
FROM Settings
WHERE employee_id=@employee_id
;";

    private const string SelectEmployeeSQL = @"
SELECT 
	id,
	user_name,
	password,
	role_name
FROM Employee
LEFT JOIN Roles ON Roles.employee_id = Employee.id
WHERE user_name = @user_name
;";

    private const string SelectEmployeeByIdSQL = @"
SELECT 
    e.id, 
    e.user_name, 
    e.password, 
    r.role_name
FROM 
    Employee e 
LEFT JOIN 
    Roles r ON e.id = r.employee_id
;";

    private const string SelectPassengerByIdSQL = @"
SELECT
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref,
    is_notifyed
FROM passengers
WHERE rowId = @id 
AND is_deleted = false
;
";
    private const string SelectAllPassengersSQL = @"
SELECT
    row_id,
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref,
    is_notifyed
FROM passengers 
WHERE is_deleted = false
ORDER BY user_id, prolongation
;
";

    private const string SelectByUserIdSQL = @"
SELECT
    row_id,
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref,
    is_notifyed
FROM passengers
WHERE user_id = @user_id
AND is_deleted = false
ORDER BY user_id, prolongation
;";

    private const string SelectExpiredPassengersSQL = @"
SELECT
    row_id,
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref,
    is_notifyed
FROM passengers
WHERE is_deleted = false
AND to_time < NOW()
AND is_notifyed = @is_notifyed
";

    private const string DeletePassengerSQL = @"
UPDATE passengers
SET is_deleted = true,
    deleted_at = NOW()
WHERE user_id = @user_id;
";

    private const string DeletePassengerByRowIdSQL = @"
UPDATE passengers
SET is_deleted = true,
    deleted_at = NOW()
WHERE row_id = @row_id;
";

    private const string SelectLastPassengerSQL = @"
SELECT 
    row_id,
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref,
    is_notifyed
FROM passengers
WHERE user_id = @user_id
AND is_deleted = false
ORDER BY prolongation DESC limit 1
";

    private const string SelectFilteredPassengersSQL = @"
SELECT
	row_id,
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref,
    is_notifyed
FROM passengers
WHERE is_deleted = false
";

    private const string UpdatePassengerSQL = @"
UPDATE passengers SET
     service_type = @service_type,
     name_surname = @name_surname,
     airline = @airline,
     flight_date=@flight_date,
     status = @status,
     from_time = @from_time,
     to_time = @to_time,
     card_number = @card_number,
     ticket_number = @ticket_number,
     guests_quantity = @guests_quantity,
     pax_id = @pax_id,
     notes = @notes,
     guest_age = @guest_age,
     transaction_number = @transaction_number,
     seat_number = @seat_number,
     voucher_number = @voucher_number,
     child_age = @child_age,
     declined_code = @declined_code,
     amount = @amount,
     CB_child = @CB_child,
     passport_number = @passport_number,
     receipt_number = @receipt_number,
     pass_ref = @pass_ref
WHERE row_id = @row_id;
";

    private const string UpdateProlongSQL = @"
UPDATE passengers SET
     service_type = @service_type,
     status = @status,
     from_time = @from_time,
     to_time = @to_time,
     flight_date= @flight_date,
     card_number = @card_number,
     guests_quantity = @guests_quantity,
     notes = @notes,
     guest_age = @guest_age,
     transaction_number = @transaction_number,
     seat_number = @seat_number,
     voucher_number = @voucher_number,
     child_age = @child_age,
     amount = @amount,
     CB_child = @CB_child,
     pass_ref = @pass_ref
WHERE row_id = @row_id;
";

    private const string SetPassengerNotifyedSQL = @"
UPDATE passengers SET
    is_notifyed = true
WHERE row_id = @row_id;
";

    #endregion

    #region Constructors

    public DbService(IOptions<PostgresConfiguration> config)
    {
        _config = config.Value;
        _connectionString =
            $"Host={_config.Host};Port={_config.Port};Username={_config.Username};Password={_config.Password};Database={_config.Database};Pooling=true;Minimum Pool Size=1;Maximum Pool Size=128;";
        InitDb();
    }

    #endregion

    #region Private Methods

    private static bool DatabaseExists(string connectionString, string databaseName)
    {
        using var dataSource = NpgsqlDataSource.Create(connectionString);
        using var command = dataSource.CreateCommand("SELECT 1 FROM pg_database WHERE datname = @name");
        command.Parameters.AddWithValue("name", databaseName);
        return command.ExecuteScalar() != null;
    }

    private void InitDb()
    {
        var connectionString =
            $"Host={_config.Host};Port={_config.Port};Username={_config.Username};Password={_config.Password};Database=postgres";

        if (!DatabaseExists(connectionString, _config.Database))
        {
            using var createDbDataSource = NpgsqlDataSource.Create(connectionString);
            using var createDbCmd = createDbDataSource.CreateCommand($"CREATE DATABASE {_config.Database};");
            createDbCmd.ExecuteNonQuery();
        }

        using var dataSource = NpgsqlDataSource.Create(_connectionString);
        using var tableCmd = dataSource.CreateCommand(CreateTablesSQL);
        tableCmd.ExecuteNonQuery();

        using var indexCmd = dataSource.CreateCommand(CreateIndexSQL);
        indexCmd.ExecuteNonQuery();
    }

    private async Task<long> GetTotalCountAsync(FilterDto filter = null)
    {
        const string baseQuerySQL = "SELECT COUNT(*) FROM passengers WHERE is_deleted = false";

        var sql = string.Empty;

        if (filter == null)
        {
            sql = baseQuerySQL;
        }
        else
        {
            var sb = new StringBuilder(baseQuerySQL);

            FillFilter(filter, sb);

            sql = sb.ToString();
        }

        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader.GetInt64(0);
        }
        return 0;
    }

    private static void FillFilter(FilterDto filter, StringBuilder sb)
    {
        if (!string.IsNullOrEmpty(filter.ServiceType))
        {
            sb.Append($" AND service_type like '%{filter.ServiceType}%'");
        }
        if (!string.IsNullOrEmpty(filter.NameSurname))
        {
            sb.Append($" AND name_surname like '%{filter.NameSurname}%'");
        }
        if (!string.IsNullOrEmpty(filter.Airline))
        {
            sb.Append($" AND airline like '%{filter.Airline}%'");
        }
        if (filter.FlightDateBegin.HasValue)
        {
            sb.Append($" AND flight_date >= '{filter.FlightDateBegin.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz")}'");
        }
        if (filter.FlightDateEnd.HasValue)
        {
            sb.Append($" AND flight_date <= '{filter.FlightDateEnd.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz")}'");
        }
        if (!string.IsNullOrEmpty(filter.Status))
        {
            sb.Append($" AND status like '%{filter.Status}%'");
        }
        if (filter.FromTimeBegin.HasValue)
        {
            sb.Append($" AND from_time >= '{filter.FromTimeBegin.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz")}'");
        }
        if (filter.FromTimeEnd.HasValue)
        {
            sb.Append($" AND from_time <= '{filter.FromTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz")}'");
        }
        if (filter.ToTimeBegin.HasValue)
        {
            sb.Append($" AND to_time >= '{filter.ToTimeBegin.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz")}'");
        }
        if (filter.ToTimeEnd.HasValue)
        {
            sb.Append($" AND to_time <= '{filter.ToTimeEnd.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz")}'");
        }
        if (!string.IsNullOrEmpty(filter.CardNumber))
        {
            sb.Append($" AND card_number like '%{filter.CardNumber}%'");
        }
        if (!string.IsNullOrEmpty(filter.TicketNumber))
        {
            sb.Append($" AND ticket_number like '%{filter.TicketNumber}%'");
        }
        if (filter.GuestQuantity.HasValue)
        {
            sb.Append($" AND guests_quantity = {filter.GuestQuantity}");
        }
        if (filter.PaxId.HasValue)
        {
            sb.Append($" AND pax_id = {filter.PaxId}");
        }
        if (filter.GuestAge.HasValue)
        {
            sb.Append($" AND guest_age = {filter.GuestAge}");
        }
        if (!string.IsNullOrEmpty(filter.TransactionNumber))
        {
            sb.Append($" AND transaction_number like '%{filter.TransactionNumber}%'");
        }
        if (!string.IsNullOrEmpty(filter.SeatNumber))
        {
            sb.Append($" AND seat_number like '%{filter.SeatNumber}%'");
        }
        if (!string.IsNullOrEmpty(filter.VoucherNumber))
        {
            sb.Append($" AND voucher_number like '%{filter.VoucherNumber}%'");
        }
        if (filter.ChildAge.HasValue)
        {
            sb.Append($" AND child_age = {filter.ChildAge}");
        }
        if (!string.IsNullOrEmpty(filter.DeclinedCode))
        {
            sb.Append($" AND declined_code like '%{filter.DeclinedCode}%'");
        }
        if (filter.Amount.HasValue)
        {
            sb.Append($" AND amount = {filter.Amount}");
        }
        if (!string.IsNullOrEmpty(filter.PassportNumber))
        {
            sb.Append($" AND passport_number like '%{filter.PassportNumber}%'");
        }
        if (!string.IsNullOrEmpty(filter.ReceiptNumber))
        {
            sb.Append($" AND receipt_number like '%{filter.ReceiptNumber}%'");
        }
        if (!string.IsNullOrEmpty(filter.PassRef))
        {
            sb.Append($" AND pass_ref like '%{filter.PassRef}%'");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Add Passenger
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<int> AddPassengerAsync(Passenger input)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(InsertPassengerSQL, connection);

        var userId = command.Parameters.Add("@user_id", NpgsqlDbType.Uuid);
        var serviceTypeParam = command.Parameters.Add("@service_type", NpgsqlDbType.Varchar);
        var nameSurnameParam = command.Parameters.Add("@name_surname", NpgsqlDbType.Varchar);
        var airlineParam = command.Parameters.Add("@airline", NpgsqlDbType.Varchar);
        var flightDateParam = command.Parameters.Add("@flight_date", NpgsqlDbType.TimestampTz);
        var statusParam = command.Parameters.Add("@status", NpgsqlDbType.Varchar);
        var prolongationParam = command.Parameters.Add("@prolongation", NpgsqlDbType.Integer);
        var fromTimeParam = command.Parameters.Add("@from_time", NpgsqlDbType.TimestampTz);
        var toTimeParam = command.Parameters.Add("@to_time", NpgsqlDbType.TimestampTz);
        var cardNumberParam = command.Parameters.Add("@card_number", NpgsqlDbType.Varchar);
        var ticketNumberParam = command.Parameters.Add("@ticket_number", NpgsqlDbType.Varchar);
        var guestsQuantityParam = command.Parameters.Add("@guests_quantity", NpgsqlDbType.Integer);
        var paxIdParam = command.Parameters.Add("@pax_id", NpgsqlDbType.Integer);
        var notesParam = command.Parameters.Add("@notes", NpgsqlDbType.Text);
        var guestAgeParam = command.Parameters.Add("@guest_age", NpgsqlDbType.Integer);
        var transactionNumberParam = command.Parameters.Add("@transaction_number", NpgsqlDbType.Varchar);
        var seatNumberParam = command.Parameters.Add("@seat_number", NpgsqlDbType.Varchar);
        var voucherNumberParam = command.Parameters.Add("@voucher_number", NpgsqlDbType.Varchar);
        var childAgeParam = command.Parameters.Add("@child_age", NpgsqlDbType.Integer);
        var declinedCodeParam = command.Parameters.Add("@declined_code", NpgsqlDbType.Varchar);
        var amountParam = command.Parameters.Add("@amount", NpgsqlDbType.Numeric);
        var cbChildParam = command.Parameters.Add("@CB_child", NpgsqlDbType.Boolean);
        var passportNumberParam = command.Parameters.Add("@passport_number", NpgsqlDbType.Varchar);
        var receiptNumberParam = command.Parameters.Add("@receipt_number", NpgsqlDbType.Varchar);
        var passRefParam = command.Parameters.Add("@pass_ref", NpgsqlDbType.Varchar);

        await command.PrepareAsync();

        userId.Value = input.UserId;
        serviceTypeParam.Value = input.ServiceType;
        nameSurnameParam.Value = input.NameSurname;
        airlineParam.Value = input.Airline;
        flightDateParam.Value = DateTime.SpecifyKind(input.FlightDate.Value, DateTimeKind.Utc);
        statusParam.Value = input.Status;
        prolongationParam.Value = input.Prolongation;
        fromTimeParam.Value = DateTime.SpecifyKind(input.FromTime.Value, DateTimeKind.Utc);
        toTimeParam.Value = DateTime.SpecifyKind(input.ToTime.Value, DateTimeKind.Utc);
        cardNumberParam.Value = input.CardNumber;
        ticketNumberParam.Value = input.TicketNumber;
        guestsQuantityParam.Value = input.GuestsQuantity;
        paxIdParam.Value = input.PaxId;
        notesParam.Value = input.Notes;
        guestAgeParam.Value = input.GuestAge;
        transactionNumberParam.Value = input.TransactionNumber;
        seatNumberParam.Value = input.SeatNumber;
        voucherNumberParam.Value = input.VoucherNumber;
        childAgeParam.Value = input.ChildAge;
        declinedCodeParam.Value = input.DeclinedCode;
        amountParam.Value = input.Amount;
        cbChildParam.Value = input.CBChild;
        passportNumberParam.Value = input.PassportNumber;
        receiptNumberParam.Value = input.ReceiptNumber;
        passRefParam.Value = input.PassRef;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> UpdatePassengerAsync(Passenger input)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(UpdatePassengerSQL, connection);

        var rowIdParam = command.Parameters.Add("@row_id", NpgsqlDbType.Bigint);
        var serviceTypeParam = command.Parameters.Add("@service_type", NpgsqlDbType.Varchar);
        var nameSurnameParam = command.Parameters.Add("@name_surname", NpgsqlDbType.Varchar);
        var airlineParam = command.Parameters.Add("@airline", NpgsqlDbType.Varchar);
        var flightDateParam = command.Parameters.Add("@flight_date", NpgsqlDbType.TimestampTz);
        var statusParam = command.Parameters.Add("@status", NpgsqlDbType.Varchar);
        var fromTimeParam = command.Parameters.Add("@from_time", NpgsqlDbType.TimestampTz);
        var toTimeParam = command.Parameters.Add("@to_time", NpgsqlDbType.TimestampTz);
        var cardNumberParam = command.Parameters.Add("@card_number", NpgsqlDbType.Varchar);
        var ticketNumberParam = command.Parameters.Add("@ticket_number", NpgsqlDbType.Varchar);
        var guestsQuantityParam = command.Parameters.Add("@guests_quantity", NpgsqlDbType.Integer);
        var paxIdParam = command.Parameters.Add("@pax_id", NpgsqlDbType.Integer);
        var notesParam = command.Parameters.Add("@notes", NpgsqlDbType.Text);
        var guestAgeParam = command.Parameters.Add("@guest_age", NpgsqlDbType.Integer);
        var transactionNumberParam = command.Parameters.Add("@transaction_number", NpgsqlDbType.Varchar);
        var seatNumberParam = command.Parameters.Add("@seat_number", NpgsqlDbType.Varchar);
        var voucherNumberParam = command.Parameters.Add("@voucher_number", NpgsqlDbType.Varchar);
        var childAgeParam = command.Parameters.Add("@child_age", NpgsqlDbType.Integer);
        var declinedCodeParam = command.Parameters.Add("@declined_code", NpgsqlDbType.Varchar);
        var amountParam = command.Parameters.Add("@amount", NpgsqlDbType.Numeric);
        var cbChildParam = command.Parameters.Add("@CB_child", NpgsqlDbType.Boolean);
        var passportNumberParam = command.Parameters.Add("@passport_number", NpgsqlDbType.Varchar);
        var receiptNumberParam = command.Parameters.Add("@receipt_number", NpgsqlDbType.Varchar);
        var passRefParam = command.Parameters.Add("@pass_ref", NpgsqlDbType.Varchar);

        await command.PrepareAsync();

        rowIdParam.Value = input.RowId;
        serviceTypeParam.Value = input.ServiceType;
        nameSurnameParam.Value = input.NameSurname;
        airlineParam.Value = input.Airline;
        flightDateParam.Value = DateTime.SpecifyKind(input.FlightDate.Value, DateTimeKind.Utc);
        statusParam.Value = input.Status;
        fromTimeParam.Value = DateTime.SpecifyKind(input.FromTime.Value, DateTimeKind.Utc);
        toTimeParam.Value = DateTime.SpecifyKind(input.ToTime.Value, DateTimeKind.Utc);
        cardNumberParam.Value = input.CardNumber;
        ticketNumberParam.Value = input.TicketNumber;
        guestsQuantityParam.Value = input.GuestsQuantity;
        paxIdParam.Value = input.PaxId;
        notesParam.Value = input.Notes;
        guestAgeParam.Value = input.GuestAge;
        transactionNumberParam.Value = input.TransactionNumber;
        seatNumberParam.Value = input.SeatNumber;
        voucherNumberParam.Value = input.VoucherNumber;
        childAgeParam.Value = input.ChildAge;
        declinedCodeParam.Value = input.DeclinedCode;
        amountParam.Value = input.Amount;
        cbChildParam.Value = input.CBChild;
        passportNumberParam.Value = input.PassportNumber;
        receiptNumberParam.Value = input.ReceiptNumber;
        passRefParam.Value = input.PassRef;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> UpdateProlongAsync(ProlongationDto input)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(UpdateProlongSQL, connection);

        var rowId = command.Parameters.Add("@row_id", NpgsqlDbType.Bigint);
        var serviceTypeParam = command.Parameters.Add("@service_type", NpgsqlDbType.Varchar);
        var statusParam = command.Parameters.Add("@status", NpgsqlDbType.Varchar);
        var fromTimeParam = command.Parameters.Add("@from_time", NpgsqlDbType.TimestampTz);
        var toTimeParam = command.Parameters.Add("@to_time", NpgsqlDbType.TimestampTz);
		var flightDateParam = command.Parameters.Add("@flight_date", NpgsqlDbType.TimestampTz);
		var cardNumberParam = command.Parameters.Add("@card_number", NpgsqlDbType.Varchar);
        var guestsQuantityParam = command.Parameters.Add("@guests_quantity", NpgsqlDbType.Integer);
        var notesParam = command.Parameters.Add("@notes", NpgsqlDbType.Text);
        var guestAgeParam = command.Parameters.Add("@guest_age", NpgsqlDbType.Integer);
        var transactionNumberParam = command.Parameters.Add("@transaction_number", NpgsqlDbType.Varchar);
        var seatNumberParam = command.Parameters.Add("@seat_number", NpgsqlDbType.Varchar);
        var voucherNumberParam = command.Parameters.Add("@voucher_number", NpgsqlDbType.Varchar);
        var childAgeParam = command.Parameters.Add("@child_age", NpgsqlDbType.Integer);
        var amountParam = command.Parameters.Add("@amount", NpgsqlDbType.Numeric);
        var cbChildParam = command.Parameters.Add("@CB_child", NpgsqlDbType.Boolean);
        var passRefParam = command.Parameters.Add("@pass_ref", NpgsqlDbType.Varchar);

        await command.PrepareAsync();

        rowId.Value = input.RowId;
        serviceTypeParam.Value = input.ServiceType;
        statusParam.Value = input.Status;
        fromTimeParam.Value = input.FromTime;
        toTimeParam.Value = input.ToTime;
        flightDateParam.Value = input.FlightDate;
		cardNumberParam.Value = input.CardNumber;
        guestsQuantityParam.Value = input.GuestQuantity;
        notesParam.Value = input.Notes;
        guestAgeParam.Value = input.GuestAge;
        transactionNumberParam.Value = input.TransactionNumber;
        seatNumberParam.Value = input.SeatNumber;
        voucherNumberParam.Value = input.VoucherNumber;
        childAgeParam.Value = input.ChildAge;
        amountParam.Value = input.Amount;
        cbChildParam.Value = input.CBChild;
        passRefParam.Value = input.PassRef;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<Passenger> GetPassengerAsync(long id)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(SelectPassengerByIdSQL, connection);

        var idParam = command.Parameters.Add("@id", NpgsqlDbType.Bigint);
        await command.PrepareAsync();

        idParam.Value = id;

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Passenger
            {
                RowId = reader.GetInt32(reader.GetOrdinal("row_id")),
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                ServiceType = reader.GetString(reader.GetOrdinal("service_type")),
                NameSurname = reader.GetString(reader.GetOrdinal("name_surname")),
                Airline = reader.GetString(reader.GetOrdinal("airline")),
                FlightDate = reader.GetDateTime(reader.GetOrdinal("flight_date")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Prolongation = reader.GetInt32(reader.GetOrdinal("prolongation")),
                FromTime = reader.GetDateTime(reader.GetOrdinal("from_time")),
                ToTime = reader.GetDateTime(reader.GetOrdinal("to_time")),
                CardNumber = reader.GetString(reader.GetOrdinal("card_number")),
                TicketNumber = reader.GetString(reader.GetOrdinal("ticket_number")),
                GuestsQuantity = reader.GetInt32(reader.GetOrdinal("guests_quantity")),
                PaxId = reader.GetInt32(reader.GetOrdinal("pax_id")),
                Notes = reader.GetString(reader.GetOrdinal("notes")),
                GuestAge = reader.GetInt32(reader.GetOrdinal("guest_age")),
                TransactionNumber = reader.GetString(reader.GetOrdinal("transaction_number")),
                SeatNumber = reader.GetString(reader.GetOrdinal("seat_number")),
                VoucherNumber = reader.GetString(reader.GetOrdinal("voucher_number")),
                ChildAge = reader.GetInt32(reader.GetOrdinal("child_age")),
                DeclinedCode = reader.GetString(reader.GetOrdinal("declined_code")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                CBChild = reader.GetBoolean(reader.GetOrdinal("cb_child")),
                PassportNumber = reader.GetString(reader.GetOrdinal("passport_number")),
                ReceiptNumber = reader.GetString(reader.GetOrdinal("receipt_number")),
                PassRef = reader.GetString(reader.GetOrdinal("pass_ref")),
            };
        }

        return null;
    }

    public async Task<List<Passenger>> GetPassengersAsync(Guid id)
    {
        var passengers = new List<Passenger>();
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(SelectByUserIdSQL, connection);

        var idParam = command.Parameters.Add("@user_id", NpgsqlDbType.Uuid);
        await command.PrepareAsync();

        idParam.Value = id;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var passenger = new Passenger
            {
                RowId = reader.GetInt32(reader.GetOrdinal("row_id")),
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                ServiceType = reader.GetString(reader.GetOrdinal("service_type")),
                NameSurname = reader.GetString(reader.GetOrdinal("name_surname")),
                Airline = reader.GetString(reader.GetOrdinal("airline")),
                FlightDate = await reader.IsDBNullAsync(reader.GetOrdinal("flight_date"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("flight_date")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Prolongation = reader.GetInt32(reader.GetOrdinal("prolongation")),
                FromTime = reader.GetDateTime(reader.GetOrdinal("from_time")),
                ToTime = reader.GetDateTime(reader.GetOrdinal("to_time")),
                CardNumber = reader.GetString(reader.GetOrdinal("card_number")),
                TicketNumber = reader.GetString(reader.GetOrdinal("ticket_number")),
                GuestsQuantity = reader.GetInt32(reader.GetOrdinal("guests_quantity")),
                PaxId = reader.GetInt32(reader.GetOrdinal("pax_id")),
                Notes = reader.GetString(reader.GetOrdinal("notes")),
                GuestAge = reader.GetInt32(reader.GetOrdinal("guest_age")),
                TransactionNumber = reader.GetString(reader.GetOrdinal("transaction_number")),
                SeatNumber = reader.GetString(reader.GetOrdinal("seat_number")),
                VoucherNumber = reader.GetString(reader.GetOrdinal("voucher_number")),
                ChildAge = reader.GetInt32(reader.GetOrdinal("child_age")),
                DeclinedCode = reader.GetString(reader.GetOrdinal("declined_code")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                CBChild = reader.GetBoolean(reader.GetOrdinal("cb_child")),
                PassportNumber = reader.GetString(reader.GetOrdinal("passport_number")),
                ReceiptNumber = reader.GetString(reader.GetOrdinal("receipt_number")),
                PassRef = reader.GetString(reader.GetOrdinal("pass_ref"))
            };

            passengers.Add(passenger);
        }

        return passengers;
    }

    public async Task<Passenger> GetLastPassengerAsync(Guid userId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(SelectLastPassengerSQL, connection);

        var uidParam = command.Parameters.Add("@user_id", NpgsqlDbType.Uuid);
        await command.PrepareAsync();

        uidParam.Value = userId;

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Passenger
            {
                RowId = reader.GetInt32(reader.GetOrdinal("row_id")),
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                ServiceType = reader.GetString(reader.GetOrdinal("service_type")),
                NameSurname = reader.GetString(reader.GetOrdinal("name_surname")),
                Airline = reader.GetString(reader.GetOrdinal("airline")),
                FlightDate = await reader.IsDBNullAsync(reader.GetOrdinal("flight_date"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("flight_date")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Prolongation = reader.GetInt32(reader.GetOrdinal("prolongation")),
                FromTime = reader.GetDateTime(reader.GetOrdinal("from_time")),
                ToTime = reader.GetDateTime(reader.GetOrdinal("to_time")),
                CardNumber = reader.GetString(reader.GetOrdinal("card_number")),
                TicketNumber = reader.GetString(reader.GetOrdinal("ticket_number")),
                GuestsQuantity = reader.GetInt32(reader.GetOrdinal("guests_quantity")),
                PaxId = reader.GetInt32(reader.GetOrdinal("pax_id")),
                Notes = reader.GetString(reader.GetOrdinal("notes")),
                GuestAge = reader.GetInt32(reader.GetOrdinal("guest_age")),
                TransactionNumber = reader.GetString(reader.GetOrdinal("transaction_number")),
                SeatNumber = reader.GetString(reader.GetOrdinal("seat_number")),
                VoucherNumber = reader.GetString(reader.GetOrdinal("voucher_number")),
                ChildAge = reader.GetInt32(reader.GetOrdinal("child_age")),
                DeclinedCode = reader.GetString(reader.GetOrdinal("declined_code")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                CBChild = reader.GetBoolean(reader.GetOrdinal("cb_child")),
                PassportNumber = reader.GetString(reader.GetOrdinal("passport_number")),
                ReceiptNumber = reader.GetString(reader.GetOrdinal("receipt_number")),
                PassRef = reader.GetString(reader.GetOrdinal("pass_ref"))
            };
        }

        return null;
    }

    public async Task<int> DeletePassengerAsync(Guid userId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(DeletePassengerSQL, connection);

        var uidParam = command.Parameters.Add("@user_id", NpgsqlDbType.Uuid);
        await command.PrepareAsync();

        uidParam.Value = userId;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteByRowIdAsync(long rowId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(DeletePassengerByRowIdSQL, connection);

        var uidParam = command.Parameters.Add("@row_id", NpgsqlDbType.Bigint);
        await command.PrepareAsync();

        uidParam.Value = rowId;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<PagedDto> GetPagedPassengersAsync(PagedFilterDto pagedFilter)
    {
        var ret = new PagedDto();
        try
        {
            var sortColumn = string.IsNullOrWhiteSpace(pagedFilter.SortColumn) && pagedFilter.SortColumn != "row_id"
                ? string.Empty
                : $" ,{pagedFilter.SortColumn}";
            var filter = pagedFilter.Filter;
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sb = new StringBuilder(@"
SELECT
    row_id,
    user_id,
    service_type,
    name_surname,
    airline,
    flight_date,
    status,
    prolongation,
    from_time,
    to_time, 
    card_number,
    ticket_number,
    guests_quantity,
    pax_id,
    notes,
    guest_age, 
    transaction_number,
    seat_number,
    voucher_number,
    child_age,
    declined_code, 
    amount,
    CB_child,
    passport_number,
    receipt_number,
    pass_ref
FROM passengers 
WHERE is_deleted = false 
");

            FillFilter(filter, sb);

            sb.Append($" ORDER BY user_id, prolongation {sortColumn} LIMIT @PageSize OFFSET @Offset;");

            await using var command = new NpgsqlCommand(sb.ToString(), connection);
            command.Parameters.AddWithValue("PageSize", pagedFilter.PageSize);
            command.Parameters.AddWithValue("Offset", (pagedFilter.PageNumber - 1) * pagedFilter.PageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var passenger = new Passenger
                {
                    RowId = reader.GetInt64(reader.GetOrdinal("row_id")),
                    UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                    ServiceType = reader.GetString(reader.GetOrdinal("service_type")),
                    NameSurname = reader.GetString(reader.GetOrdinal("name_surname")),
                    Airline = reader.GetString(reader.GetOrdinal("airline")),
                    FlightDate = await reader.IsDBNullAsync(reader.GetOrdinal("flight_date"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("flight_date")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    Prolongation = reader.GetInt32(reader.GetOrdinal("prolongation")),
                    FromTime = reader.GetDateTime(reader.GetOrdinal("from_time")),
                    ToTime = reader.GetDateTime(reader.GetOrdinal("to_time")),
                    CardNumber = reader.GetString(reader.GetOrdinal("card_number")),
                    TicketNumber = reader.GetString(reader.GetOrdinal("ticket_number")),
                    GuestsQuantity = reader.GetInt32(reader.GetOrdinal("guests_quantity")),
                    PaxId = reader.GetInt32(reader.GetOrdinal("pax_id")),
                    Notes = reader.GetString(reader.GetOrdinal("notes")),
                    GuestAge = reader.GetInt32(reader.GetOrdinal("guest_age")),
                    TransactionNumber = reader.GetString(reader.GetOrdinal("transaction_number")),
                    SeatNumber = reader.GetString(reader.GetOrdinal("seat_number")),
                    VoucherNumber = reader.GetString(reader.GetOrdinal("voucher_number")),
                    ChildAge = reader.GetInt32(reader.GetOrdinal("child_age")),
                    DeclinedCode = reader.GetString(reader.GetOrdinal("declined_code")),
                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                    CBChild = reader.GetBoolean(reader.GetOrdinal("CB_child")),
                    PassportNumber = reader.GetString(reader.GetOrdinal("passport_number")),
                    ReceiptNumber = reader.GetString(reader.GetOrdinal("receipt_number")),
                    PassRef = reader.GetString(reader.GetOrdinal("pass_ref"))
                };
                ret.Items.Add(passenger);
            }

            ret.TotalCount = await GetTotalCountAsync(filter);
        }
        catch (Exception ex)
        {

        }
        return ret;
    }

    public async Task<List<Passenger>> GetExpiredPassengersAsync(bool isNotified = false)
    {
        var ret = new List<Passenger>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(SelectExpiredPassengersSQL, connection);

        var isNotifiedParam = command.Parameters.Add("@is_notifyed", NpgsqlDbType.Boolean);

        await command.PrepareAsync();

        isNotifiedParam.Value = isNotified;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var passenger = new Passenger
            {
                RowId = reader.GetInt64(reader.GetOrdinal("row_id")),
                UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                ServiceType = reader.GetString(reader.GetOrdinal("service_type")),
                NameSurname = reader.GetString(reader.GetOrdinal("name_surname")),
                Airline = reader.GetString(reader.GetOrdinal("airline")),
                FlightDate = await reader.IsDBNullAsync(reader.GetOrdinal("flight_date"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("flight_date")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Prolongation = reader.GetInt32(reader.GetOrdinal("prolongation")),
                FromTime = reader.GetDateTime(reader.GetOrdinal("from_time")),
                ToTime = reader.GetDateTime(reader.GetOrdinal("to_time")),
                CardNumber = reader.GetString(reader.GetOrdinal("card_number")),
                TicketNumber = reader.GetString(reader.GetOrdinal("ticket_number")),
                GuestsQuantity = reader.GetInt32(reader.GetOrdinal("guests_quantity")),
                PaxId = reader.GetInt32(reader.GetOrdinal("pax_id")),
                Notes = reader.GetString(reader.GetOrdinal("notes")),
                GuestAge = reader.GetInt32(reader.GetOrdinal("guest_age")),
                TransactionNumber = reader.GetString(reader.GetOrdinal("transaction_number")),
                SeatNumber = reader.GetString(reader.GetOrdinal("seat_number")),
                VoucherNumber = reader.GetString(reader.GetOrdinal("voucher_number")),
                ChildAge = reader.GetInt32(reader.GetOrdinal("child_age")),
                DeclinedCode = reader.GetString(reader.GetOrdinal("declined_code")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                CBChild = reader.GetBoolean(reader.GetOrdinal("CB_child")),
                PassportNumber = reader.GetString(reader.GetOrdinal("passport_number")),
                ReceiptNumber = reader.GetString(reader.GetOrdinal("receipt_number")),
                PassRef = reader.GetString(reader.GetOrdinal("pass_ref"))
            };
            ret.Add(passenger);
        }

        return ret;
    }
    public async Task<int> SetPassengerNotifyedAsync(long rowId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(SetPassengerNotifyedSQL, connection);

        var rowIdParam = command.Parameters.Add("@row_id", NpgsqlDbType.Bigint);
        await command.PrepareAsync();

        rowIdParam.Value = rowId;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> AddSettingsAsync(Settings input)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(InsertSettingsSQL, connection);

        var employee_id = command.Parameters.Add("@employee_id", NpgsqlDbType.Bigint);
        var gridsettingsjson = command.Parameters.Add("@grid_settings_json", NpgsqlDbType.Text);

        await command.PrepareAsync();

        employee_id.Value = input.EmployeeId;
        gridsettingsjson.Value = input.GridSettingsJson;

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<Settings> GetSettingsAsync(long employeeId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(SelectSettingsSQL, connection);

        var employeeIdParam = command.Parameters.Add("@employee_id", NpgsqlDbType.Bigint);
        await command.PrepareAsync();

        employeeIdParam.Value = employeeId;

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Settings
            {
                EmployeeId = reader.GetInt64(reader.GetOrdinal("employee_id")),
                GridSettingsJson = reader.GetString(reader.GetOrdinal("grid_settings_json"))
            };
        }

        return null;
    }


    public async Task<int> AddOrUpdateEmployeeAsync(EmployeeDto input)
    {
        try
        {
            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var connection = await dataSource.OpenConnectionAsync();

            await using var command = new NpgsqlCommand(InsertOrUpdateEmployeeWithRolesSQL, connection);

            command.Parameters.Add("@user_name", NpgsqlDbType.Varchar).Value = input.UserName;
            command.Parameters.Add("@password", NpgsqlDbType.Varchar).Value = input.Password;
            command.Parameters.Add("@roles", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = input.Roles.ToArray();

            await command.PrepareAsync();
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return -1;
        }
    }

    public async Task<int> DeleteEmployeeAsync(string userNamr)
    {
        try
        {
            await using var dataSource = NpgsqlDataSource.Create(_connectionString);
            await using var connection = await dataSource.OpenConnectionAsync();

            await using var command = new NpgsqlCommand(DeleteEmployeeSQL, connection);

            var user_name = command.Parameters.Add("@user_name", NpgsqlDbType.Text);
            await command.PrepareAsync();

            user_name.Value = userNamr;

            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return -1;
        }
    }

    public async Task<EmployeeDto> GetEmployeeAsync(string userName)
    {
        var employee = new EmployeeDto();
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(SelectEmployeeSQL, connection);

        var empidParam = command.Parameters.Add("@user_name", NpgsqlDbType.Text);
        await command.PrepareAsync();

        empidParam.Value = userName;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            employee.Id = reader.GetInt64(reader.GetOrdinal("id"));
            employee.UserName = reader.GetString(reader.GetOrdinal("user_name"));
            employee.Password = reader.GetString(reader.GetOrdinal("password"));
            if (!await reader.IsDBNullAsync(reader.GetOrdinal("role_name")))
            {
                employee.Roles.Add(reader.GetString(reader.GetOrdinal("role_name")));
            }
        }

        return employee;
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync()
    {
        var employees = new Dictionary<long, EmployeeDto>();
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(SelectEmployeeByIdSQL, connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt64(reader.GetOrdinal("id"));
            if (!employees.TryGetValue(id, out var employee))
            {
                employee = new EmployeeDto
                {
                    Id = id,
                    UserName = reader.GetString(reader.GetOrdinal("user_name")),
                    Password = reader.GetString(reader.GetOrdinal("password")),
                    Roles = new List<string>()
                };
                employees[id] = employee;
            }

            if (!await reader.IsDBNullAsync(reader.GetOrdinal("role_name")))
            {
                employee.Roles.Add(reader.GetString(reader.GetOrdinal("role_name")));
            }
        }
        return employees.Values.ToList();
    }
};

#endregion
