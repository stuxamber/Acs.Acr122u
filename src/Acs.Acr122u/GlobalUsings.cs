// Project-wide usings. Keeping these centralized avoids repeating boilerplate `using` blocks in
// every file and keeps the public-facing files focused on the API surface itself.

global using System;
global using System.Buffers.Binary;
global using System.Collections.Generic;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Runtime.Versioning;
global using System.Threading;
global using System.Threading.Tasks;

global using Acs.Acr122u.Apdu;
global using Acs.Acr122u.Commands;
global using Acs.Acr122u.Diagnostics;
global using Acs.Acr122u.Exceptions;
global using Acs.Acr122u.Mifare;
global using Acs.Acr122u.Models;
global using Acs.Acr122u.Transport;
