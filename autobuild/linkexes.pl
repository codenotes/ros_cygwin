#!/usr/bin/perl
$suffix = '_isolated' if -d "build_isolated" && -d "install_isolated";

die "build$suffix folder does not exist." unless -d "build$suffix";
die "install$suffix folder does not exist." unless -d "install$suffix";

print "Scanning packages...\n";
foreach $pkg (`ls -1 devel$suffix`)
{
	chomp $pkg;
	$bindir = "devel$suffix/$pkg/bin";
	next unless -d $bindir;
	foreach $exe(`ls -1 $bindir`)
	{
		chomp $exe;
		$exe_to_pkg{$exe} = $pkg;
	}
}

print "Symlinking executables...";
foreach $exe (`ls -1 install$suffix/bin`)
{
	chomp $exe;
	$pkg = $exe_to_pkg{$exe};
	if ($pkg ne '')
	{
		mkdir "install$suffix/share/$pkg";
		symlink("../../bin/$exe", "install$suffix/share/$pkg/$exe");
	}
}
print " done\n";