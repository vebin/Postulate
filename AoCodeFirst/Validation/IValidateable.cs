using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postulate.Validation
{
	public interface IValidateable
	{
		IEnumerable<string> Validate(IDbConnection connection = null);
	}
}
