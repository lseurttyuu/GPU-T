using Xunit;
using System.IO;
using GPU_T.Services.Utilities;

namespace GPU_T.Tests.Services.Utilities;

public class GpuFeatureDetectionTests
{
    // This test covers the parsing logic for kernel driver dates from various formats of /proc/version content.
    [Theory]
    [InlineData("Linux version 6.12.43+ (root@debian-mike) (gcc (Debian 14.2.0-19) 14.2.0, GNU ld (GNU Binutils for Debian) 2.44) #1 SMP PREEMPT_DYNAMIC Sun Sep 21 22:36:29 CEST 2025", "21 Sep 2025")]
    [InlineData("Linux version 6.19.0-2-cachyos (linux-cachyos@cachyos) (clang version 21.1.6, LLD 21.1.6) #1 SMP PREEMPT_DYNAMIC Fri, 13 Feb 2026 22:57:05 +0000", "13 Feb 2026")]
    [InlineData("Linux version 6.19.2-pikaos (root@61b54b6642f4) (Debian clang version 21.1.8 (1 d94f8c4acd920b4eae297348eafe867318d960fb), Debian LLD 21.1.8 (https://git.pika-os.com/repo-tools/run-upstream-build d94f8c4acd920b4eae297348eafe867318d960fb)) #101pika9 SMP PREEMPT_DYNAMIC Tue Feb 17 07:19:14 EST 2026", "17 Feb 2026")]
    [InlineData("Linux version 6.18.12+deb14-amd64 (debian-kernel@lists.debian.org) (x86_64-linux-gnu-gcc-15 (Debian 15.2.0-13) 15.2.0, GNU ld (GNU Binutils for Debian) 2.46) #1 SMP PREEMPT_DYNAMIC Debian 6.18.12-1 (2026-02-17) ", "2026-02-17")]
    [InlineData("Linux version 6.18.12+deb14-amd64 (debian-kernel@lists.debian.org) (x86_64-linux-gnu-gcc-15 (Debian 15.2.0-13) 15.2.0, GNU ld (GNU Binutils for Debian) 2.46) #1 SMP PREEMPT_DYNAMIC Debian 6.18.12-1 (2026/02/17) ", "2026/02/17")]
    [InlineData("Linux version 6.18.12+deb14-amd64 (debian-kernel@lists.debian.org) (x86_64-linux-gnu-gcc-15 (Debian 15.2.0-13) 15.2.0, GNU ld (GNU Binutils for Debian) 2.46) #1 SMP PREEMPT_DYNAMIC Debian 6.18.12-1 (17/02/2026) ", "17/02/2026")]
    [InlineData("Linux version 5.4.0-150-generic (buildd@bos03-amd64-012) (gcc version 7.5.0 (Ubuntu 7.5.0-3ubuntu1~18.04)) #167~18.04.1-Ubuntu SMP Wed May 24 00:51:42 UTC 2023", "24 May 2023")]
    [InlineData("Linux version 6.8.0-100-generic (buildd@lcy02-amd64-008) (x86_64-linux-gnu-gcc-12 (Ubuntu 12.3.0-1ubuntu1~22.04.2) 12.3.0, GNU ld (GNU Binutils for Ubuntu) 2.38) #100~22.04.1-Ubuntu SMP PREEMPT_DYNAMIC Mon Jan 19 17:10:19 UTC ", "19 Jan 2026")]
    [InlineData("Linux version 6.17.0-14-generic (buildd@lcy02-amd64-067) (x86_64-linux-gnu-gcc-13 (Ubuntu 13.3.0-6ubuntu2~24.04) 13.3.0, GNU ld (GNU Binutils for Ubuntu) 2.42) #14~24.04.1-Ubuntu SMP PREEMPT_DYNAMIC Thu Jan 15 15:52:10 UTC 2", "15 Jan 2026")]
    [InlineData("Linux version 6.6.10 (...) #1 SMP PREEMPT_DYNAMIC Some random string", "N/A")]
    [InlineData("Linux version without hash symbol at all", "N/A")]
    public void GetKernelDriverDate_ParsesDifferentFormatsCorrectly(string fileContent, string expectedDate)
    {
        string result = GpuFeatureDetection.ParseKernelDate(fileContent, () => "2026");

        Assert.Equal(expectedDate, result);
    }
}