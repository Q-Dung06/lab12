import os
from dulwich.repo import Repo
import dulwich.porcelain as porcelain

lab12_dir = r"d:\Lập trình .NET\lab12"
repo = Repo(lab12_dir)

# Gather all files relative to lab12_dir (excluding .git, bin, obj, .vs)
files_to_add = []
exclude_dirs = {".git", "bin", "obj", ".vs"}

for root, dirs, files in os.walk(lab12_dir):
    # prune excluded dirs
    dirs[:] = [d for d in dirs if d not in exclude_dirs]
    for f in files:
        if f.endswith(".pyc") or f.startswith(".git"):
            continue
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, lab12_dir)
        files_to_add.append(rel_path)

print(f"Total files found to stage: {len(files_to_add)}")

# Stage files
porcelain.add(repo, paths=files_to_add)

idx = repo.open_index()
print(f"Files now in index: {len(idx)}")

# Commit
msg = b"Add complete Lab 12 solution, database scripts, report and screenshots"
commit_id = porcelain.commit(repo, message=msg, author=b"Q-Dung06 <24003351+Q-Dung06@users.noreply.github.com>")
print("Committed successfully! Commit ID:", commit_id.decode("ascii"))

# Push
token = os.environ.get("GH_TOKEN", "")
if not token:
    print("Error: No GH_TOKEN!")
else:
    remote_url = f"https://Q-Dung06:{token}@github.com/Q-Dung06/lab12.git"
    print("Pushing to GitHub...")
    porcelain.push(repo, remote_location=remote_url, refspecs=[b"HEAD:refs/heads/main"])
    print("PUSH COMPLETED SUCCESSFULLY!")
