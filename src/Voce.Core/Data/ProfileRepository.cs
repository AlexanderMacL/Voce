using Microsoft.Data.Sqlite;
using Voce.Core.Models;

namespace Voce.Core.Data;

public sealed class ProfileRepository(VoceDatabase db)
{
    public List<Profile> GetAll()
    {
        using var c = db.Open();
        return c.Query("SELECT * FROM profiles ORDER BY is_default DESC, label", Map);
    }

    public Profile? Get(long id)
    {
        using var c = db.Open();
        return c.Query("SELECT * FROM profiles WHERE id = $id", Map, ("$id", id)).FirstOrDefault();
    }

    public Profile? GetDefault()
    {
        using var c = db.Open();
        return c.Query("SELECT * FROM profiles ORDER BY is_default DESC, id LIMIT 1", Map).FirstOrDefault();
    }

    public void Save(Profile p)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        var args = new (string, object?)[]
        {
            ("$id", p.Id), ("$label", p.Label), ("$singer", p.SingerName), ("$voice", p.VoicePart),
            ("$address", p.Address), ("$email", p.Email), ("$phone", p.Phone), ("$utr", p.Utr),
            ("$bank", p.BankName), ("$acname", p.AccountName), ("$sort", p.SortCode), ("$acno", p.AccountNumber),
            ("$terms", p.PaymentTermsDays), ("$tpl", p.FirstLineTemplate), ("$def", p.IsDefault ? 1 : 0),
        };
        if (p.Id == 0)
        {
            p.Id = c.Insert("""
                INSERT INTO profiles (label, singer_name, voice_part, address, email, phone, utr, bank_name,
                    account_name, sort_code, account_number, payment_terms_days, first_line_template, is_default)
                VALUES ($label, $singer, $voice, $address, $email, $phone, $utr, $bank, $acname, $sort, $acno, $terms, $tpl, $def)
                """, args);
        }
        else
        {
            c.Execute("""
                UPDATE profiles SET label=$label, singer_name=$singer, voice_part=$voice, address=$address, email=$email,
                    phone=$phone, utr=$utr, bank_name=$bank, account_name=$acname, sort_code=$sort, account_number=$acno,
                    payment_terms_days=$terms, first_line_template=$tpl, is_default=$def
                WHERE id=$id
                """, args);
        }
        if (p.IsDefault)
            c.Execute("UPDATE profiles SET is_default = 0 WHERE id <> $id", ("$id", p.Id));
        else if (c.Scalar<long>("SELECT COUNT(*) FROM profiles WHERE is_default = 1") == 0)
        {
            // Always keep exactly one default.
            c.Execute("UPDATE profiles SET is_default = 1 WHERE id = $id", ("$id", p.Id));
            p.IsDefault = true;
        }
        tx.Commit();
    }

    public void SetDefault(long id)
    {
        using var c = db.Open();
        c.Execute("UPDATE profiles SET is_default = CASE WHEN id = $id THEN 1 ELSE 0 END", ("$id", id));
    }

    public void Delete(long id)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        c.Execute("DELETE FROM profiles WHERE id = $id", ("$id", id));
        if (c.Scalar<long>("SELECT COUNT(*) FROM profiles WHERE is_default = 1") == 0)
            c.Execute("UPDATE profiles SET is_default = 1 WHERE id = (SELECT MIN(id) FROM profiles)");
        tx.Commit();
    }

    internal static Profile Map(SqliteDataReader r) => new()
    {
        Id = r.Long("id"),
        Label = r.Str("label"),
        SingerName = r.Str("singer_name"),
        VoicePart = r.Str("voice_part"),
        Address = r.Str("address"),
        Email = r.Str("email"),
        Phone = r.Str("phone"),
        Utr = r.Str("utr"),
        BankName = r.Str("bank_name"),
        AccountName = r.Str("account_name"),
        SortCode = r.Str("sort_code"),
        AccountNumber = r.Str("account_number"),
        PaymentTermsDays = (int)r.Long("payment_terms_days"),
        FirstLineTemplate = r.Str("first_line_template"),
        IsDefault = r.Bool("is_default"),
    };
}
