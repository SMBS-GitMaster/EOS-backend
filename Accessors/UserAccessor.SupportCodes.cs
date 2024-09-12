using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeZoneConverter;

namespace RadialReview.Accessors {

	public class SecretCode {
		public DateTime Date { get; set; }
		public string Code { get; set; }
	}

	public partial class UserAccessor : BaseAccessor {

		public static SecretCode ResetSupportCode(UserOrganizationModel caller, long forUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin(true);
					var user = s.Get<UserOrganizationModel>(forUserId);
					if (user != null) {
						user.LastSupportCodeReset = DateTime.UtcNow;
						s.Update(user);
						user.UpdateCache(s);
						tx.Commit();
						s.Flush();
						return SupportCodeAlpha_Unsafe(user);
					}

					return null;
				}
			}
		}

		public static IEnumerable<SecretCode> GetSupportSecretCodes(UserOrganizationModel caller, long forUserId) {
      var forUser = caller;
			if (caller.Id == forUserId || PermissionsUtility.TestIsAdmin(caller)) {
				if (caller.Id != forUserId) {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							forUser = s.Get<UserOrganizationModel>(forUserId);
						}
					}
				}

				if (forUser == null || forUser.User == null || forUser.User.Id == null) {
					yield break;
				}

				if (forUser.LastSupportCodeReset != null && forUser.LastSupportCodeReset.Value.IsBetween(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow)) {
					yield return SupportCodeAlpha_Unsafe(forUser);
				}

				TimeZoneInfo cstZone = TZConvert.GetTimeZoneInfo("America/Chicago");
				var date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone).Date;
				var i = 0;
				while (i < 1000) {
					yield return SupportCodeNumeric_Unsafe(forUser, i, date);
					i += 1;
				}
			}

			yield break;
		}

    public static Dictionary<long,SecretCode> GetSupportSecretCodeForUsers(UserOrganizationModel caller, IEnumerable<long> userIds)
    {
      using var session = HibernateSession.GetCurrentSession();
      var userSecretCodes = new Dictionary<long, SecretCode>();

      if (PermissionsUtility.TestIsAdmin(caller))
      {
        var users = session.QueryOver<UserOrganizationModel>()
          .WhereRestrictionOn(user => user.Id).IsIn(userIds.ToArray())
          .And(user => user.User != null)
          .List().ToList();

        TimeZoneInfo cstZone = TZConvert.GetTimeZoneInfo("America/Chicago");
        var date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone).Date;
        var previousDayUtc = DateTime.UtcNow.AddDays(-1);

        foreach (var user in users)
        {
          if (user.LastSupportCodeReset != null && user.LastSupportCodeReset.Value.IsBetween(previousDayUtc, DateTime.UtcNow))
          {
            var secretCode = SupportCodeAlpha_Unsafe(user);
            userSecretCodes.Add(user.Id, secretCode);
          }
          else
          {
            var secretCode = SupportCodeNumeric_Unsafe(user, daysAgo: 0, date);
            userSecretCodes.Add(user.Id, secretCode);
          }
        }
      }
      else if (userIds.Contains(caller.Id))
      {
        var secretCode = GetSupportSecretCodes(caller, caller.Id).FirstOrDefault();
        userSecretCodes.Add(caller.Id, secretCode);
      }

      return userSecretCodes;
    }

    private static int Secret1 = HashUtil.GetDeterministicHashCode("69d06930-8454-47c8-ad3b-a1daeb5cda04");
		private static int Secret2 = HashUtil.GetDeterministicHashCode("1328f980-d002-40cd-9b9a-be631a3be161");
		private static char[] ALPHABET = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

		private static List<int> GeneratePrimes() {
			return new List<int> { 10949243, 8888147, 17935361, 9222331, 2270693, 17027509, 4878431, 8259389, 14618449, 4228271, 288499, 8380693, 12645949, 12558779, 2408587, 9491467, 3068419, 1728689, 6014563, 7319203, 1804133, 1334341, 8626903, 19918133, 878387, 2531017, 7624973, 640589, 8396951, 10662737, 17997121, 8494987, 19134397, 3099203, 5910703, 17694389, 7759187, 18548641, 2799779, 8342029, 6699607, 7944373, 15327581, 1036649, 18123097, 1599461, 3843841, 12098861, 16015409, 9213319, 314351, 2901571, 14872019, 14730733, 5433157, 3752039, 4865629, 3375767, 16838629, 16723997, 10702441, 12194263, 13576331, 9001351, 17248499, 620743, 5589937, 19041809, 14650903, 11750993, 3408413, 17503891, 1873133, 19861159, 9667079, 19957577, 6241043, 473939, 11470757, 15964187, 4127897, 6318131, 5901223, 1318739, 13603151, 10525717, 11656877, 1895869, 8601883, 18320879, 1144519, 106619, 10737589, 4420567, 15595183, 17497421, 2559751, 571583, 10545737, 13883579, 7327543, 13900559, 16077101, 3108949, 4571467, 14705477, 2311123, 2264329, 7922197, 1495717, 19938179, 16828289, 9934877, 4819169, 5704033, 10343117, 2151869, 18467707, 14018377, 4667567, 18349679, 10529741, 14219269, 13404673, 12940127, 3260291, 13731467, 9186797, 8093333, 2494537, 17842351, 19206893, 7295461, 13848187, 18909929, 17897317, 3219179, 12429751, 2318501, 3726313, 769243, 5894873, 5535067, 6155593, 8480471, 9445001, 6184741, 12575839, 666277, 15928499, 16605619, 8919077, 17561123, 19968997, 10220921, 11574119, 13703297, 4523521, 21787, 10163851, 2773403, 19538501, 3471103, 10643459, 15003757, 2809091, 19724279, 14868019, 941027, 13498091, 9488981, 5203907, 13913819, 4612723, 3132559, 18443839, 7563761, 14955509, 8625853, 9283259, 14732833, 2154811, 13589179, 6457337, 600091, 12904349, 10914599, 379, 6865343, 12623977, 10468177, 10585699, 7903367, 12322451, 5076413, 6036731, 12888047, 14549669, 4011113, 3609013, 16236163, 10022059, 14585267, 11537531, 3425951, 7086109, 11595677, 6169087, 18809083, 8938871, 14082589, 17746483, 13497199, 13927493, 5190859, 17955781, 14874311, 2360851, 14007223, 2575663, 8588189, 6102869, 17343097, 14389721, 19415911, 1445413, 18017771, 13342729, 12333697, 18951043, 16371401, 10016323, 792397, 8159659, 10840657, 1872929, 7979773, 8110859, 10082797, 11685073, 2832449, 3879049, 10174319, 2430257, 14088211, 11232709, 541999, 9862063, 7498481, 14595391, 15625373, 9563293, 18826333, 4635437, 14988767, 16839457, 1312681, 5452523, 3678967, 7747081, 6843379, 18591151, 462493, 1964461, 16059181, 16185641, 9535319, 5505133, 16279859, 7174949, 13642199, 19324667, 16051361, 15572707, 4454741, 144311, 15656393, 9769451, 4725607, 4178791, 18856693, 4321231, 13401307, 6243791, 13045889, 6238157, 17335867, 7760171, 7572989, 14056019, 17243357, 11496101, 11290271, 19371419, 2656211, 11715139, 9633797, 4773709, 16978769, 14592587, 3689137, 9168941, 18542593, 4588373, 14227841, 8523131, 4347313, 19605731, 1158491, 12710657, 14855437, 2777213, 10620367, 580837, 16726621, 831091, 569897, 8088947, 469691, 531833, 10603591, 2139953, 434981, 236261, 9193417, 2076619, 12684563, 9775763, 12716593, 1372667, 13686767, 11719471, 3398063, 7624273, 5149537, 1267067, 9171233, 19310197, 15704803, 14691583, 1329499, 1684873, 10647739, 18115813, 16428493, 4737367, 4930381, 9144259, 1014721, 12972571, 13601461, 1282577, 12772777, 2573509, 2347369, 14504767, 547229, 8966869, 12098627, 2122513, 12343057, 11637233, 8968943, 18489059, 8114453, 14838647, 13751407, 11899721, 15874007, 15925709, 14682197, 1068377, 6865927, 11961401, 10531039, 19872661, 5106719, 18111013, 15552113, 3846197, 13002131, 15132563, 5940581, 8006749, 8184199, 9436169, 16501337, 18766723, 493277, 2700601, 9774617, 5179303, 6528517, 6868871, 19449659, 7773911, 15089119 };
		}
		private static string GetCharacterAt(int idx, ref string str) {
			try {
				var loc = idx % str.Length;
				var output = str.Substring(loc, 1);
				var temp = str.Substring(0, loc);
				if (loc + 1 < str.Length)
					temp += str.Substring(loc + 1, str.Length - (1 + loc));
				str = temp;
				return output;
			} catch (Exception e) {
				return "0";
			}

		}

		public static SecretCode SupportCodeNumeric_Unsafe(UserOrganizationModel forUser, int daysAgo, DateTime nowDate) {
			long cstTime = nowDate.ToJsMs() / 86400000;

			var rnd = Math.Abs(HashUtil.Merge(HashUtil.GetDeterministicHashCode(forUser.User.Id), HashUtil.GetDeterministicHashCode(((int)((cstTime - daysAgo) % int.MaxValue)).ToString()), Secret1));
			string code = PseudoRandomizeString("" + rnd);
			return new SecretCode() { Date = nowDate.AddDays(-daysAgo), Code = code };
		}


		public static SecretCode SupportCodeAlpha_Unsafe(UserOrganizationModel forUser) {
			var hash = Math.Abs(HashUtil.Merge(HashUtil.GetDeterministicHashCode(forUser.User.Id), HashUtil.GetDeterministicHashCode(forUser.LastSupportCodeReset.Value.ToString()), Secret2));
			var passcode = IntToString(hash, ALPHABET);
			var code = PseudoRandomizeString(passcode);
			return new SecretCode() { Date = forUser.LastSupportCodeReset.Value, Code = code };
		}

		private static string PseudoRandomizeString(string str) {
			var rndCode = "" + str;
			var code = "";
			var characterIdx = GeneratePrimes();
			var jump = characterIdx[Math.Abs(HashUtil.GetDeterministicHashCode(str)) % characterIdx.Count];

			for (var j = 0; j < 6; j++) {
				var idx = (jump * j) % characterIdx.Count;
				var prime = characterIdx[idx];
				characterIdx.RemoveAt(idx);
				code += GetCharacterAt(prime, ref rndCode);
			}
			return code;
		}

		private static string IntToString(int value, char[] baseChars) {
			string result = string.Empty;
			int targetBase = baseChars.Length;

			do {
				result = baseChars[value % targetBase] + result;
				value = value / targetBase;
			}
			while (value > 0);

			return result;
		}


	}
}
