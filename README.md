# Postulate

1/10/17 -- I've been interested in ORM libraries for a long time, and I've been resistant to the code-first approach for quite a while as well. "Postulate" represents my attempt to make peace with code-first by doing it My Way. The [Clobber](github.com/adamosoftware/clobber) project, a database-first ORM library, is on hold for the now.

- I have never been comfortable with Entity Framework migrations -- I find them simply hard to use, but I also dislike the behavior of silently dropping and rebuilding tables at run time. I would rather implement database changes more deliberately rather than have objects drop and rebuild on their own.

- I never saw a good way in EF to implement model-wide conventions such as date and user stamp fields. For example, I usually have DateCreated and DateModified columns. I would like those columns to inherit from a base class, and have some standard insert and update behavior that is defined once only.

- My experience is with multi-tenant systems, and with my ORM library, I wanted a good way to 



I must also say I'm not crazy about Linq as a replacement for general
