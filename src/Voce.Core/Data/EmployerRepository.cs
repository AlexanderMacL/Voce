using Microsoft.Data.Sqlite;
using Voce.Core.Models;

namespace Voce.Core.Data;

public sealed class EmployerRepository(VoceDatabase db)
{
    /// <summary>All employers with their events, ordered by name.</summary>
    public List<Employer> GetAll(bool includeArchived = true)
    {
        using var c = db.Open();
        var employers = c.Query(
            "SELECT * FROM employers" + (includeArchived ? "" : " WHERE is_archived = 0") + " ORDER BY name COLLATE NOCASE",
            MapEmployer);
        var events = c.Query("SELECT * FROM events ORDER BY name COLLATE NOCASE", MapEvent).ToLookup(e => e.EmployerId);
        foreach (var e in employers)
            e.Events = events[e.Id].Where(ev => includeArchived || !ev.IsArchived).ToList();
        return employers;
    }

    public Employer? Get(long id) => GetAll().FirstOrDefault(e => e.Id == id);

    /// <summary>Saves the employer and replaces its event list with <see cref="Employer.Events"/>.</summary>
    public void Save(Employer e)
    {
        using var c = db.Open();
        using var tx = c.BeginTransaction();
        var args = new (string, object?)[]
        {
            ("$id", e.Id), ("$name", e.Name), ("$contact", e.ContactName), ("$address", e.Address),
            ("$email", e.Email), ("$notes", e.Notes), ("$arch", e.IsArchived ? 1 : 0),
        };
        if (e.Id == 0)
            e.Id = c.Insert("INSERT INTO employers (name, contact_name, address, email, notes, is_archived) VALUES ($name, $contact, $address, $email, $notes, $arch)", args);
        else
            c.Execute("UPDATE employers SET name=$name, contact_name=$contact, address=$address, email=$email, notes=$notes, is_archived=$arch WHERE id=$id", args);

        var keep = e.Events.Where(ev => ev.Id != 0).Select(ev => ev.Id).ToList();
        c.Execute($"DELETE FROM events WHERE employer_id = $eid AND id NOT IN ({string.Join(",", keep.DefaultIfEmpty(-1))})", ("$eid", e.Id));
        foreach (var ev in e.Events)
        {
            ev.EmployerId = e.Id;
            var evArgs = new (string, object?)[]
            {
                ("$id", ev.Id), ("$eid", e.Id), ("$name", ev.Name), ("$fee", Money.ToPence(ev.DefaultFee)), ("$arch", ev.IsArchived ? 1 : 0),
            };
            if (ev.Id == 0)
                ev.Id = c.Insert("INSERT INTO events (employer_id, name, default_fee_pence, is_archived) VALUES ($eid, $name, $fee, $arch)", evArgs);
            else
                c.Execute("UPDATE events SET name=$name, default_fee_pence=$fee, is_archived=$arch WHERE id=$id", evArgs);
        }
        tx.Commit();
    }

    /// <summary>Deletes an employer and its events. Existing invoices keep their bill-to snapshot.</summary>
    public void Delete(long id)
    {
        using var c = db.Open();
        c.Execute("DELETE FROM employers WHERE id = $id", ("$id", id));
    }

    public int CountInvoices(long employerId)
    {
        using var c = db.Open();
        return (int)c.Scalar<long>("SELECT COUNT(*) FROM invoices WHERE employer_id = $id", ("$id", employerId));
    }

    private static Employer MapEmployer(SqliteDataReader r) => new()
    {
        Id = r.Long("id"),
        Name = r.Str("name"),
        ContactName = r.Str("contact_name"),
        Address = r.Str("address"),
        Email = r.Str("email"),
        Notes = r.Str("notes"),
        IsArchived = r.Bool("is_archived"),
    };

    private static EventType MapEvent(SqliteDataReader r) => new()
    {
        Id = r.Long("id"),
        EmployerId = r.Long("employer_id"),
        Name = r.Str("name"),
        DefaultFee = Money.FromPence(r.NLong("default_fee_pence")),
        IsArchived = r.Bool("is_archived"),
    };
}
