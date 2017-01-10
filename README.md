# Postulate

**1/10/17** -- I've been interested in ORM libraries for a long time, and I've been resistant to the code-first approach for quite a while as well. "Postulate" represents my attempt to make peace with code-first by doing it My Way. My [Clobber](https://github.com/adamosoftware/Clobber) project, a database-first ORM library, is on hold for now. Here are some design goals and rationales for Postulate:

- I have never been comfortable with Entity Framework migrations -- I find them simply hard to use, but I also dislike the behavior of silently dropping and rebuilding tables at run time. I would rather implement database changes more deliberately rather than have objects drop and rebuild on their own.

- I never saw a good way in EF to implement model-wide conventions such as date and user stamp fields. For example, I usually have DateCreated and DateModified columns. I would like those columns to inherit from a base class, and be able to define standard insert and update behavior that applies to these columns no matter what table they appear in.

- My experience is with multi-tenant systems. With my ORM library, I wanted a good way to mark the "customer ID" discriminator field as "insert-only" so that it's impossible for data to move between customers unintentionally.

When I'm ready, I'll post code samples and a Nuget package.
